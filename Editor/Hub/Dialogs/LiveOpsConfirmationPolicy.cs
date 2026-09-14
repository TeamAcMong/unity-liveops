using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Thao tác sửa cần quyết mức xác nhận (bảng 7.0). Chỉ thêm giá trị vào cuối, không đổi số (luật enum của package).</summary>
    internal enum LiveOpsEditOperation
    {
        DeleteFixedEvent = 0,
        ChangeFixedEventTimes = 1,
        RenameOrRetypeFixedEvent = 2,
        /// <summary>Tiền tố, neo, chu kỳ — những thứ quyết định id của từng lần lặp.</summary>
        ChangeRecurringIdentity = 3,
        ChangeRecurringActiveHours = 4,
        RemoveRecurringRule = 5,
        RemoveEventType = 6,
        EditEventTypeFields = 7,
        ReplaceDraftWithJson = 8,
        RestorePublishedIntoDraft = 9,
        RemovePublishedStamp = 10,
        OverwriteDiskChanges = 11,
        MarkPublished = 12,
        ApplySafeRepair = 13,
        ApplyProposal = 14,
        IgnoreWarning = 15,
        AddFixedEvent = 16,
        AddRecurringRule = 17,
        /// <summary>V-12: đưa làn lên/xuống — không đổi JSON, không hỏi.</summary>
        ReorderEventTypes = 18,
        /// <summary>V-15: bỏ bỏ qua — cảnh báo hiện lại, không mất gì.</summary>
        RemoveIgnoredWarning = 19,
        /// <summary>V-14: chưa có asset nên không có nháp để mất.</summary>
        ImportRunningJsonIntoNewAsset = 20,
    }

    internal enum LiveOpsConfirmRequirement
    {
        None = 0,
        Level1 = 1,
        TypeToConfirm = 2,
        DedicatedDialog = 3,
        NotAllowed = 4,
    }

    /// <summary>Kết quả của <see cref="LiveOpsConfirmationPolicy.Decide"/> — presenter đọc để mở hộp đúng cấp hoặc tắt nút kèm lý do.</summary>
    internal sealed class LiveOpsConfirmDecision
    {
        internal LiveOpsConfirmDecision(LiveOpsConfirmRequirement requirement, string reasonCode, string runningEventId,
            DateTime? runningEndUtc, int inUseCount)
        {
            Requirement = requirement;
            ReasonCode = reasonCode ?? string.Empty;
            RunningEventId = runningEventId ?? string.Empty;
            // Cấp 2 luôn gõ đúng id của đợt/lần lặp đang chạy (8.6) — không có mức nào khác cần chuỗi gõ.
            TypeToConfirmText = requirement == LiveOpsConfirmRequirement.TypeToConfirm ? RunningEventId : string.Empty;
            RunningEndUtc = runningEndUtc;
            InUseCount = inUseCount;
        }

        public LiveOpsConfirmRequirement Requirement { get; }

        /// <summary>Id đợt/lần lặp đang chạy phải gõ (cấp 2), "" ở mức khác.</summary>
        public string TypeToConfirmText { get; }

        /// <summary>Một hằng <c>Reason…</c> của <see cref="LiveOpsConfirmationPolicy"/> — view chọn câu theo mã, không parse chữ.</summary>
        public string ReasonCode { get; }

        public string RunningEventId { get; }
        public DateTime? RunningEndUtc { get; }

        /// <summary>Số đợt + luật còn dùng loại khi xoá loại bị chặn ("Xoá loại (còn 3 đợt)"); 0 ở thao tác khác.</summary>
        public int InUseCount { get; }
    }

    /// <summary>
    /// MỘT model quyết mức xác nhận cho mọi màn (Lịch, Luật lặp, Kiểm lịch, Xuất JSON, Dán) — PD-24, bảng 7.0. View không
    /// tự hỏi; presenter gọi <see cref="Decide"/> rồi mới mở hộp qua <c>services.Confirmation</c>, nên hai màn sửa cùng một
    /// thứ không bao giờ hỏi hai kiểu khác nhau.
    /// <para>
    /// "Đang chạy" = <c>start &lt;= now &lt; end</c> tính trên NHÁP TRƯỚC KHI SỬA (sửa rồi mới tính thì kéo đợt đang chạy ra
    /// khỏi <c>now</c> trông như "chưa bắt đầu" và lọt qua hộp). Đợt cố định đọc thẳng giờ trong tài liệu; lần lặp tính từ
    /// chính luật (neo, chu kỳ, thời gian chạy, tiền tố) như runtime sinh id. "Đã đăng" = bản so có đợt cùng id.
    /// Giờ không đọc được = đợt không đặt được lên lịch game = không đang chạy.
    /// </para>
    /// <para>
    /// Ngoại lệ có chủ đích (mục 7.4, "so bản so nếu có"): đổi tiền tố/neo/chu kỳ và xoá luật lặp lấy lần lặp đang chạy từ
    /// luật cùng loại trong BẢN SO khi bản so có luật đó — người chơi đang giữ id của bản đã đăng, không phải id của nháp.
    /// Tính trên nháp thì hoàn tiền tố về đúng bản đã đăng vẫn bị bắt gõ, và chữ phải gõ là id nháp (<c>pass-35</c>) chứ
    /// không phải id người chơi có (<c>weekly-pass-35</c>). Bản so không bị lệnh sửa đụng tới nên vẫn giữ được lý do của
    /// "tính trước khi sửa". Không có bản so hoặc bản so không có luật đó thì quay về nháp trước khi sửa (hỏi thừa an toàn
    /// hơn bỏ sót).
    /// </para>
    /// </summary>
    internal static class LiveOpsConfirmationPolicy
    {
        public const string ReasonRunning = "running";
        public const string ReasonPublishedNotStarted = "published-not-started";
        public const string ReasonNotStartedUnpublished = "not-started-unpublished";
        public const string ReasonNotStarted = "not-started";
        public const string ReasonEnded = "ended";
        public const string ReasonShortensRunning = "shortens-running";
        public const string ReasonExtendsRunning = "extends-running";
        public const string ReasonRunningStartLocked = "running-start-locked";
        public const string ReasonRunningIdKept = "running-id-kept";
        public const string ReasonNoRunningOccurrence = "no-running-occurrence";
        public const string ReasonTypeInUse = "type-in-use";
        public const string ReasonTypeUnused = "type-unused";
        public const string ReasonAlways = "always";
        public const string ReasonPreviewed = "previewed";
        public const string ReasonNothingToLose = "nothing-to-lose";
        public const string ReasonTargetMissing = "target-missing";

        /// <param name="draftBefore">Nháp trước khi sửa — bắt buộc.</param>
        /// <param name="draftAfter">Nháp sau khi sửa — bắt buộc với đổi giờ/đổi id-loại/đổi luật; null được với xoá và thao tác không đích.</param>
        /// <param name="publishedBaseline">Bản so đã đăng (<c>ActiveBaseline</c>); null = chưa có dấu đã đăng.</param>
        /// <param name="targetKey"><c>EntryKey</c> của đợt cố định; <c>EventType</c> của luật lặp và của loại; bỏ qua với thao tác không đích.</param>
        public static LiveOpsConfirmDecision Decide(LiveOpsEditOperation operation, LiveEventCalendarDocument draftBefore,
            LiveEventCalendarDocument draftAfter, LiveEventCalendarDocument publishedBaseline, DateTime nowUtc, string targetKey)
        {
            if (draftBefore == null) throw new ArgumentNullException(nameof(draftBefore));
            DateTime now = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
            switch (operation)
            {
                case LiveOpsEditOperation.DeleteFixedEvent:
                    return DecideDeleteFixedEvent(draftBefore, publishedBaseline, now, targetKey);
                case LiveOpsEditOperation.ChangeFixedEventTimes:
                    return DecideChangeFixedEventTimes(draftBefore, RequireAfter(draftAfter), publishedBaseline, now, targetKey);
                case LiveOpsEditOperation.RenameOrRetypeFixedEvent:
                    return DecideRenameOrRetypeFixedEvent(draftBefore, RequireAfter(draftAfter), now, targetKey);
                case LiveOpsEditOperation.ChangeRecurringIdentity:
                    return DecideChangeRecurringIdentity(draftBefore, RequireAfter(draftAfter), publishedBaseline, now, targetKey);
                case LiveOpsEditOperation.ChangeRecurringActiveHours:
                    return DecideChangeRecurringActiveHours(draftBefore, RequireAfter(draftAfter), now, targetKey);
                case LiveOpsEditOperation.RemoveRecurringRule:
                    return DecideRemoveRecurringRule(draftBefore, publishedBaseline, now, targetKey);
                case LiveOpsEditOperation.RemoveEventType:
                    return DecideRemoveEventType(draftBefore, targetKey);
                case LiveOpsEditOperation.EditEventTypeFields:
                    return Simple(LiveOpsConfirmRequirement.None, ReasonAlways);
                case LiveOpsEditOperation.ReplaceDraftWithJson:
                case LiveOpsEditOperation.RestorePublishedIntoDraft:
                case LiveOpsEditOperation.RemovePublishedStamp:
                case LiveOpsEditOperation.OverwriteDiskChanges:
                    // Thay/khôi phục/gỡ dấu/ghi đè đĩa làm mất nhiều mục một lúc — hộp cấp 1 nêu từng id sẽ mất.
                    return Simple(LiveOpsConfirmRequirement.Level1, ReasonAlways);
                case LiveOpsEditOperation.MarkPublished:
                    return Simple(LiveOpsConfirmRequirement.DedicatedDialog, ReasonAlways);
                case LiveOpsEditOperation.ApplySafeRepair:
                case LiveOpsEditOperation.ApplyProposal:
                case LiveOpsEditOperation.IgnoreWarning:
                case LiveOpsEditOperation.AddFixedEvent:
                case LiveOpsEditOperation.AddRecurringRule:
                    // Người dùng đã thấy xem trước/popover trước khi bấm, và toast có Hoàn tác — hỏi thêm chỉ là nhiễu.
                    return Simple(LiveOpsConfirmRequirement.None, ReasonPreviewed);
                case LiveOpsEditOperation.ReorderEventTypes:
                case LiveOpsEditOperation.RemoveIgnoredWarning:
                case LiveOpsEditOperation.ImportRunningJsonIntoNewAsset:
                    return Simple(LiveOpsConfirmRequirement.None, ReasonNothingToLose);
                default:
                    // Giá trị enum lạ (thêm mới mà quên ô bảng): hỏi cấp 1 an toàn hơn im lặng áp thay đổi.
                    return Simple(LiveOpsConfirmRequirement.Level1, ReasonAlways);
            }
        }

        /// <summary>Nhãn tiếng Việt của mức — cùng chữ với bảng 7.0 cho tooltip và palette.</summary>
        public static string LabelOf(LiveOpsConfirmRequirement requirement)
        {
            switch (requirement)
            {
                case LiveOpsConfirmRequirement.None: return LiveOpsHubStrings.KitRequirementNone;
                case LiveOpsConfirmRequirement.Level1: return LiveOpsHubStrings.KitRequirementLevel1;
                case LiveOpsConfirmRequirement.TypeToConfirm: return LiveOpsHubStrings.KitRequirementTypeToConfirm;
                case LiveOpsConfirmRequirement.DedicatedDialog: return LiveOpsHubStrings.KitRequirementDedicatedDialog;
                case LiveOpsConfirmRequirement.NotAllowed: return LiveOpsHubStrings.KitRequirementNotAllowed;
                default: return string.Empty;
            }
        }

        private static LiveOpsConfirmDecision DecideDeleteFixedEvent(LiveEventCalendarDocument draftBefore,
            LiveEventCalendarDocument publishedBaseline, DateTime now, string entryKey)
        {
            if (!draftBefore.TryGetFixedEvent(entryKey, out FixedLiveEventEntry entry)) return Simple(LiveOpsConfirmRequirement.None, ReasonTargetMissing);
            FixedPhase phase = PhaseOf(entry, now, out DateTime endUtc);
            if (phase == FixedPhase.Running)
            {
                return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.TypeToConfirm, ReasonRunning, entry.EventId, endUtc, 0);
            }
            // Q-14: đợt đã khép không bao giờ vào lại (RetiredEventIds), nên xoá không làm ai mất tiến độ — xoá thẳng + Hoàn tác.
            if (phase == FixedPhase.Ended) return Simple(LiveOpsConfirmRequirement.None, ReasonEnded);
            if (IsPublished(publishedBaseline, entry.EventId)) return Simple(LiveOpsConfirmRequirement.Level1, ReasonPublishedNotStarted);
            return Simple(LiveOpsConfirmRequirement.None, ReasonNotStartedUnpublished);
        }

        private static LiveOpsConfirmDecision DecideChangeFixedEventTimes(LiveEventCalendarDocument draftBefore,
            LiveEventCalendarDocument draftAfter, LiveEventCalendarDocument publishedBaseline, DateTime now, string entryKey)
        {
            if (!draftBefore.TryGetFixedEvent(entryKey, out FixedLiveEventEntry before)) return Simple(LiveOpsConfirmRequirement.None, ReasonTargetMissing);
            // Nháp sau không còn đợt: với người chơi đó là xoá, nên quyết theo ô xoá thay vì cho qua như đổi giờ.
            if (!draftAfter.TryGetFixedEvent(entryKey, out FixedLiveEventEntry after))
            {
                return DecideDeleteFixedEvent(draftBefore, publishedBaseline, now, entryKey);
            }
            FixedPhase phase = PhaseOf(before, now, out DateTime beforeEndUtc);
            if (phase == FixedPhase.Ended) return Simple(LiveOpsConfirmRequirement.NotAllowed, ReasonEnded);
            if (phase != FixedPhase.Running) return Simple(LiveOpsConfirmRequirement.None, ReasonNotStarted);

            before.TryGetStartUtc(out DateTime beforeStartUtc);
            // Mép đầu đợt đang chạy khoá: người chơi đã vào theo giờ bắt đầu cũ, dời nó là đổi lịch sử đã xảy ra.
            if (!after.TryGetStartUtc(out DateTime afterStartUtc) || afterStartUtc != beforeStartUtc)
            {
                return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.NotAllowed, ReasonRunningStartLocked, before.EventId, beforeEndUtc, 0);
            }
            // Giờ kết thúc mới không đọc được thì game bỏ hẳn đợt — ít nhất cũng là rút ngắn.
            if (!after.TryGetEndUtc(out DateTime afterEndUtc) || afterEndUtc < beforeEndUtc)
            {
                return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.Level1, ReasonShortensRunning, before.EventId, beforeEndUtc, 0);
            }
            return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.None, ReasonExtendsRunning, before.EventId, beforeEndUtc, 0);
        }

        private static LiveOpsConfirmDecision DecideRenameOrRetypeFixedEvent(LiveEventCalendarDocument draftBefore,
            LiveEventCalendarDocument draftAfter, DateTime now, string entryKey)
        {
            if (!draftBefore.TryGetFixedEvent(entryKey, out FixedLiveEventEntry before)) return Simple(LiveOpsConfirmRequirement.None, ReasonTargetMissing);
            FixedPhase phase = PhaseOf(before, now, out DateTime beforeEndUtc);
            if (phase == FixedPhase.Ended) return Simple(LiveOpsConfirmRequirement.None, ReasonEnded);
            if (phase != FixedPhase.Running) return Simple(LiveOpsConfirmRequirement.None, ReasonNotStarted);
            bool unchanged = draftAfter.TryGetFixedEvent(entryKey, out FixedLiveEventEntry after)
                && string.Equals(after.EventId, before.EventId, StringComparison.Ordinal)
                && string.Equals(after.EventType, before.EventType, StringComparison.Ordinal);
            if (unchanged)
            {
                return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.None, ReasonRunningIdKept, before.EventId, beforeEndUtc, 0);
            }
            // Dữ liệu người chơi khoá theo id đợt: đổi id/loại khi đang chạy = người chơi mất tiến độ — cấp 2 gõ id cũ.
            return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.TypeToConfirm, ReasonRunning, before.EventId, beforeEndUtc, 0);
        }

        private static LiveOpsConfirmDecision DecideChangeRecurringIdentity(LiveEventCalendarDocument draftBefore,
            LiveEventCalendarDocument draftAfter, LiveEventCalendarDocument publishedBaseline, DateTime now, string eventType)
        {
            if (!draftBefore.TryGetRecurringRule(eventType, out RecurringLiveEventRule before)) return Simple(LiveOpsConfirmRequirement.None, ReasonTargetMissing);
            RecurringLiveEventRule reference = ReferenceRuleForPlayers(before, publishedBaseline, eventType);
            if (!TryGetRunningOccurrence(reference, now, out LiveEventInstance running))
            {
                return Simple(LiveOpsConfirmRequirement.None, ReasonNoRunningOccurrence);
            }
            bool idKept = draftAfter.TryGetRecurringRule(eventType, out RecurringLiveEventRule after)
                && TryGetRunningOccurrence(after, now, out LiveEventInstance afterRunning)
                && string.Equals(afterRunning.EventId, running.EventId, StringComparison.Ordinal);
            if (idKept)
            {
                return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.None, ReasonRunningIdKept, running.EventId, running.EndUtc, 0);
            }
            return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.TypeToConfirm, ReasonRunning, running.EventId, running.EndUtc, 0);
        }

        private static LiveOpsConfirmDecision DecideChangeRecurringActiveHours(LiveEventCalendarDocument draftBefore,
            LiveEventCalendarDocument draftAfter, DateTime now, string eventType)
        {
            if (!draftBefore.TryGetRecurringRule(eventType, out RecurringLiveEventRule before)) return Simple(LiveOpsConfirmRequirement.None, ReasonTargetMissing);
            if (!TryGetRunningOccurrence(before, now, out LiveEventInstance running))
            {
                return Simple(LiveOpsConfirmRequirement.None, ReasonNoRunningOccurrence);
            }
            // Tìm CÙNG lần lặp (cùng id) ở luật sau: thời gian chạy mới không hợp lệ thì game bỏ cả luật — coi như rút ngắn.
            bool found = draftAfter.TryGetRecurringRule(eventType, out RecurringLiveEventRule after)
                && TryFindOccurrenceById(after, running.EventId, running.StartUtc, out LiveEventInstance afterOccurrence)
                && afterOccurrence.EndUtc >= running.EndUtc;
            if (found)
            {
                return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.None, ReasonExtendsRunning, running.EventId, running.EndUtc, 0);
            }
            return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.Level1, ReasonShortensRunning, running.EventId, running.EndUtc, 0);
        }

        private static LiveOpsConfirmDecision DecideRemoveRecurringRule(LiveEventCalendarDocument draftBefore,
            LiveEventCalendarDocument publishedBaseline, DateTime now, string eventType)
        {
            if (!draftBefore.TryGetRecurringRule(eventType, out RecurringLiveEventRule rule)) return Simple(LiveOpsConfirmRequirement.None, ReasonTargetMissing);
            RecurringLiveEventRule reference = ReferenceRuleForPlayers(rule, publishedBaseline, eventType);
            if (!TryGetRunningOccurrence(reference, now, out LiveEventInstance running))
            {
                return Simple(LiveOpsConfirmRequirement.None, ReasonNoRunningOccurrence);
            }
            return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.TypeToConfirm, ReasonRunning, running.EventId, running.EndUtc, 0);
        }

        /// <summary>
        /// Luật sinh ra id người chơi đang giữ: luật cùng loại trong bản so nếu có (7.4), không thì luật trong nháp trước khi sửa.
        /// </summary>
        private static RecurringLiveEventRule ReferenceRuleForPlayers(RecurringLiveEventRule draftBeforeRule,
            LiveEventCalendarDocument publishedBaseline, string eventType)
        {
            if (publishedBaseline != null && publishedBaseline.TryGetRecurringRule(eventType, out RecurringLiveEventRule publishedRule))
            {
                return publishedRule;
            }
            return draftBeforeRule;
        }

        private static LiveOpsConfirmDecision DecideRemoveEventType(LiveEventCalendarDocument draftBefore, string typeId)
        {
            int inUseCount = 0;
            IReadOnlyList<FixedLiveEventEntry> fixedEvents = draftBefore.FixedEvents;
            for (int index = 0; index < fixedEvents.Count; index++)
            {
                if (string.Equals(fixedEvents[index].EventType, typeId, StringComparison.Ordinal)) inUseCount++;
            }
            IReadOnlyList<RecurringLiveEventRule> rules = draftBefore.RecurringRules;
            for (int index = 0; index < rules.Count; index++)
            {
                if (string.Equals(rules[index].EventType, typeId, StringComparison.Ordinal)) inUseCount++;
            }
            // Không cho xoá loại còn dùng: xoá định nghĩa mà giữ đợt thì game bỏ mọi đợt đó (unknown-event-type) im lặng.
            if (inUseCount > 0)
            {
                return new LiveOpsConfirmDecision(LiveOpsConfirmRequirement.NotAllowed, ReasonTypeInUse, string.Empty, null, inUseCount);
            }
            return Simple(LiveOpsConfirmRequirement.None, ReasonTypeUnused);
        }

        private enum FixedPhase
        {
            Unplaceable = 0,
            NotStarted = 1,
            Running = 2,
            Ended = 3,
        }

        private static FixedPhase PhaseOf(FixedLiveEventEntry entry, DateTime now, out DateTime endUtc)
        {
            endUtc = default;
            if (!entry.TryGetStartUtc(out DateTime startUtc) || !entry.TryGetEndUtc(out endUtc)) return FixedPhase.Unplaceable;
            if (now < startUtc) return FixedPhase.NotStarted;
            return now < endUtc ? FixedPhase.Running : FixedPhase.Ended;
        }

        private static bool IsPublished(LiveEventCalendarDocument publishedBaseline, string eventId)
        {
            if (publishedBaseline == null) return false;
            IReadOnlyList<FixedLiveEventEntry> publishedEvents = publishedBaseline.FixedEvents;
            for (int index = 0; index < publishedEvents.Count; index++)
            {
                if (string.Equals(publishedEvents[index].EventId, eventId, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static bool TryGetRunningOccurrence(RecurringLiveEventRule rule, DateTime now, out LiveEventInstance running)
        {
            running = null;
            if (now == DateTime.MaxValue) return false;
            if (!TryCreateRuleCalendar(rule, out RecurringLiveEventCalendar calendar)) return false;
            IReadOnlyList<LiveEventInstance> instances;
            if (!TryGetInstances(calendar, now, now.AddTicks(1), out instances)) return false;
            for (int index = 0; index < instances.Count; index++)
            {
                LiveEventInstance instance = instances[index];
                if (instance.StartUtc <= now && now < instance.EndUtc)
                {
                    running = instance;
                    return true;
                }
            }
            return false;
        }

        private static bool TryFindOccurrenceById(RecurringLiveEventRule rule, string eventId, DateTime aroundStartUtc, out LiveEventInstance occurrence)
        {
            occurrence = null;
            if (aroundStartUtc == DateTime.MaxValue) return false;
            if (!TryCreateRuleCalendar(rule, out RecurringLiveEventCalendar calendar)) return false;
            if (!TryGetInstances(calendar, aroundStartUtc, aroundStartUtc.AddTicks(1), out IReadOnlyList<LiveEventInstance> instances)) return false;
            for (int index = 0; index < instances.Count; index++)
            {
                if (string.Equals(instances[index].EventId, eventId, StringComparison.Ordinal))
                {
                    occurrence = instances[index];
                    return true;
                }
            }
            return false;
        }

        private static bool TryCreateRuleCalendar(RecurringLiveEventRule rule, out RecurringLiveEventCalendar calendar)
        {
            calendar = null;
            if (!rule.TryGetAnchorUtc(out DateTime anchorUtc)) return false;
            if (rule.PeriodHours <= 0 || rule.ActiveHours <= 0 || rule.ActiveHours > rule.PeriodHours) return false;
            try
            {
                calendar = new RecurringLiveEventCalendar(rule.EventType, anchorUtc, TimeSpan.FromHours(rule.PeriodHours),
                    TimeSpan.FromHours(rule.ActiveHours), rule.EffectiveIdPrefix, rule.ConfigKey);
                return true;
            }
            // Luật hỏng (loại rỗng/có '#', số giờ tràn TimeSpan) thì game cũng bỏ luật: không có lần lặp nào đang chạy.
            catch (ArgumentException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }

        private static bool TryGetInstances(RecurringLiveEventCalendar calendar, DateTime fromUtc, DateTime toUtc, out IReadOnlyList<LiveEventInstance> instances)
        {
            try
            {
                instances = calendar.GetInstances(calendar.EventType, fromUtc, toUtc);
                return true;
            }
            // Tiền tố có '#' hoặc xuống dòng làm id lần lặp không hợp lệ — runtime bỏ luật đó, policy coi như không chạy.
            catch (ArgumentException)
            {
                instances = Array.Empty<LiveEventInstance>();
                return false;
            }
        }

        private static LiveEventCalendarDocument RequireAfter(LiveEventCalendarDocument draftAfter)
        {
            if (draftAfter == null) throw new ArgumentException(LiveOpsHubStrings.KitErrorPolicyDraftAfterMissing, nameof(draftAfter));
            return draftAfter;
        }

        private static LiveOpsConfirmDecision Simple(LiveOpsConfirmRequirement requirement, string reasonCode)
        {
            return new LiveOpsConfirmDecision(requirement, reasonCode, string.Empty, null, 0);
        }
    }
}
