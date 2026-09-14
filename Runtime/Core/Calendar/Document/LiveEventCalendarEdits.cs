using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Áp một <see cref="LiveEventCalendarEdit"/> lên một tài liệu — thuần, không đụng Unity, không ném. Sửa trỏ tới
    /// mục không còn (vd Undo đã xoá đợt đó) trả nguyên tài liệu gốc + <c>false</c>, để gọi nơi gọi tự quyết định
    /// (toast lỗi, bỏ qua thao tác…) thay vì crash cửa sổ hub.
    /// </summary>
    public static class LiveEventCalendarEdits
    {
        public static bool TryApply(LiveEventCalendarDocument document, LiveEventCalendarEdit edit, out LiveEventCalendarDocument result)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (edit == null) throw new ArgumentNullException(nameof(edit));

            result = document;

            switch (edit)
            {
                case AddFixedEventEdit addFixedEvent:
                    return TryAddFixedEvent(document, addFixedEvent.Entry, out result);
                case ReplaceFixedEventEdit replaceFixedEvent:
                    return TryReplaceFixedEvent(document, replaceFixedEvent.Entry, out result);
                case RemoveFixedEventEdit removeFixedEvent:
                    return TryRemoveFixedEvent(document, removeFixedEvent.EntryKey, out result);
                case SetRecurringRuleEdit setRecurringRule:
                    return TrySetRecurringRule(document, setRecurringRule.Rule, out result);
                case RemoveRecurringRuleEdit removeRecurringRule:
                    return TryRemoveRecurringRule(document, removeRecurringRule.EventType, out result);
                case SetEventTypeEdit setEventType:
                    return TrySetEventType(document, setEventType.Type, out result);
                case RemoveEventTypeEdit removeEventType:
                    return TryRemoveEventType(document, removeEventType.TypeId, out result);
                case MoveEventTypeEdit moveEventType:
                    return TryMoveEventType(document, moveEventType.TypeId, moveEventType.NewIndex, out result);
                case SetRemoteConfigKeyEdit setRemoteConfigKey:
                    result = Rebuild(document, remoteConfigKey: setRemoteConfigKey.RemoteConfigKey);
                    return true;
                case AddPublishedStampEdit addPublishedStamp:
                    return TryAddPublishedStamp(document, addPublishedStamp.Stamp, out result);
                case RemoveLatestPublishedStampEdit _:
                    return TryRemoveLatestPublishedStamp(document, out result);
                case AddIgnoredWarningEdit addIgnoredWarning:
                    return TryAddIgnoredWarning(document, addIgnoredWarning.Warning, out result);
                case RemoveIgnoredWarningEdit removeIgnoredWarning:
                    return TryRemoveIgnoredWarning(document, removeIgnoredWarning.Warning, out result);
                case ReplaceDocumentEdit replaceDocument:
                    if (replaceDocument.Document == null) return false;
                    result = replaceDocument.Document;
                    return true;
                case CompositeCalendarEdit composite:
                    return TryApplyComposite(document, composite, out result);
                default:
                    return false;
            }
        }

        private static bool TryApplyComposite(LiveEventCalendarDocument document, CompositeCalendarEdit composite,
            out LiveEventCalendarDocument result)
        {
            LiveEventCalendarDocument current = document;
            for (int index = 0; index < composite.Edits.Count; index++)
            {
                if (!TryApply(current, composite.Edits[index], out LiveEventCalendarDocument next))
                {
                    result = document;
                    return false;
                }
                current = next;
            }
            result = current;
            return true;
        }

        private static bool TryAddFixedEvent(LiveEventCalendarDocument document, FixedLiveEventEntry entry,
            out LiveEventCalendarDocument result)
        {
            result = document;
            if (entry == null || document.IndexOfFixedEvent(entry.EntryKey) >= 0) return false;

            var fixedEvents = new List<FixedLiveEventEntry>(document.FixedEvents) { entry };
            result = Rebuild(document, fixedEvents: fixedEvents);
            return true;
        }

        private static bool TryReplaceFixedEvent(LiveEventCalendarDocument document, FixedLiveEventEntry entry,
            out LiveEventCalendarDocument result)
        {
            result = document;
            if (entry == null) return false;
            int index = document.IndexOfFixedEvent(entry.EntryKey);
            if (index < 0) return false;

            var fixedEvents = new List<FixedLiveEventEntry>(document.FixedEvents);
            fixedEvents[index] = entry;
            result = Rebuild(document, fixedEvents: fixedEvents);
            return true;
        }

        private static bool TryRemoveFixedEvent(LiveEventCalendarDocument document, string entryKey,
            out LiveEventCalendarDocument result)
        {
            result = document;
            int index = document.IndexOfFixedEvent(entryKey);
            if (index < 0) return false;

            var fixedEvents = new List<FixedLiveEventEntry>(document.FixedEvents);
            fixedEvents.RemoveAt(index);
            result = Rebuild(document, fixedEvents: fixedEvents);
            return true;
        }

        private static bool TrySetRecurringRule(LiveEventCalendarDocument document, RecurringLiveEventRule rule,
            out LiveEventCalendarDocument result)
        {
            result = document;
            if (rule == null) return false;

            var recurringRules = new List<RecurringLiveEventRule>(document.RecurringRules);
            int index = IndexOfRecurringRule(recurringRules, rule.EventType);
            if (index >= 0) recurringRules[index] = rule;
            else recurringRules.Add(rule);

            result = Rebuild(document, recurringRules: recurringRules);
            return true;
        }

        private static bool TryRemoveRecurringRule(LiveEventCalendarDocument document, string eventType,
            out LiveEventCalendarDocument result)
        {
            result = document;
            var recurringRules = new List<RecurringLiveEventRule>(document.RecurringRules);
            int index = IndexOfRecurringRule(recurringRules, eventType);
            if (index < 0) return false;

            recurringRules.RemoveAt(index);
            result = Rebuild(document, recurringRules: recurringRules);
            return true;
        }

        private static bool TrySetEventType(LiveEventCalendarDocument document, LiveEventTypeDefinition type,
            out LiveEventCalendarDocument result)
        {
            result = document;
            if (type == null) return false;

            var eventTypes = new List<LiveEventTypeDefinition>(document.EventTypes);
            int index = IndexOfEventType(eventTypes, type.TypeId);
            if (index >= 0) eventTypes[index] = type;
            else eventTypes.Add(type);

            result = Rebuild(document, eventTypes: eventTypes);
            return true;
        }

        private static bool TryRemoveEventType(LiveEventCalendarDocument document, string typeId,
            out LiveEventCalendarDocument result)
        {
            result = document;
            var eventTypes = new List<LiveEventTypeDefinition>(document.EventTypes);
            int index = IndexOfEventType(eventTypes, typeId);
            if (index < 0) return false;

            eventTypes.RemoveAt(index);
            result = Rebuild(document, eventTypes: eventTypes);
            return true;
        }

        private static bool TryMoveEventType(LiveEventCalendarDocument document, string typeId, int newIndex,
            out LiveEventCalendarDocument result)
        {
            result = document;
            var eventTypes = new List<LiveEventTypeDefinition>(document.EventTypes);
            int index = IndexOfEventType(eventTypes, typeId);
            if (index < 0) return false;

            int clampedIndex = newIndex < 0 ? 0 : newIndex;
            if (clampedIndex > eventTypes.Count - 1) clampedIndex = eventTypes.Count - 1;

            LiveEventTypeDefinition moved = eventTypes[index];
            eventTypes.RemoveAt(index);
            eventTypes.Insert(clampedIndex, moved);

            result = Rebuild(document, eventTypes: eventTypes);
            return true;
        }

        private static bool TryAddPublishedStamp(LiveEventCalendarDocument document, PublishedCalendarStamp stamp,
            out LiveEventCalendarDocument result)
        {
            result = document;
            if (stamp == null) return false;

            var stamps = new List<PublishedCalendarStamp>(document.PublishedStamps) { stamp };
            result = Rebuild(document, publishedStamps: stamps);
            return true;
        }

        private static bool TryRemoveLatestPublishedStamp(LiveEventCalendarDocument document, out LiveEventCalendarDocument result)
        {
            result = document;
            if (document.PublishedStamps.Count == 0) return false;

            var stamps = new List<PublishedCalendarStamp>(document.PublishedStamps);
            stamps.RemoveAt(stamps.Count - 1);
            result = Rebuild(document, publishedStamps: stamps);
            return true;
        }

        private static bool TryAddIgnoredWarning(LiveEventCalendarDocument document, IgnoredCalendarWarning warning,
            out LiveEventCalendarDocument result)
        {
            result = document;
            if (warning == null) return false;

            var warnings = new List<IgnoredCalendarWarning>(document.IgnoredWarnings) { warning };
            result = Rebuild(document, ignoredWarnings: warnings);
            return true;
        }

        private static bool TryRemoveIgnoredWarning(LiveEventCalendarDocument document, IgnoredCalendarWarning warning,
            out LiveEventCalendarDocument result)
        {
            result = document;
            if (warning == null) return false;

            var warnings = new List<IgnoredCalendarWarning>(document.IgnoredWarnings);
            int index = warnings.IndexOf(warning);
            if (index < 0) index = IndexOfEquivalentWarning(warnings, warning);
            if (index < 0) return false;

            warnings.RemoveAt(index);
            result = Rebuild(document, ignoredWarnings: warnings);
            return true;
        }

        private static int IndexOfRecurringRule(List<RecurringLiveEventRule> rules, string eventType)
        {
            for (int index = 0; index < rules.Count; index++)
            {
                if (string.Equals(rules[index].EventType, eventType, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private static int IndexOfEventType(List<LiveEventTypeDefinition> types, string typeId)
        {
            for (int index = 0; index < types.Count; index++)
            {
                if (string.Equals(types[index].TypeId, typeId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private static int IndexOfEquivalentWarning(List<IgnoredCalendarWarning> warnings, IgnoredCalendarWarning target)
        {
            for (int index = 0; index < warnings.Count; index++)
            {
                IgnoredCalendarWarning candidate = warnings[index];
                if (string.Equals(candidate.RuleId, target.RuleId, StringComparison.Ordinal) &&
                    string.Equals(candidate.TargetId, target.TargetId, StringComparison.Ordinal) &&
                    string.Equals(candidate.RangeStartUtcText, target.RangeStartUtcText, StringComparison.Ordinal) &&
                    string.Equals(candidate.RangeEndUtcText, target.RangeEndUtcText, StringComparison.Ordinal) &&
                    string.Equals(candidate.ExpiresUtcText, target.ExpiresUtcText, StringComparison.Ordinal))
                {
                    return index;
                }
            }
            return -1;
        }

        private static LiveEventCalendarDocument Rebuild(LiveEventCalendarDocument document, string remoteConfigKey = null,
            List<LiveEventTypeDefinition> eventTypes = null, List<RecurringLiveEventRule> recurringRules = null,
            List<FixedLiveEventEntry> fixedEvents = null, List<PublishedCalendarStamp> publishedStamps = null,
            List<IgnoredCalendarWarning> ignoredWarnings = null)
        {
            return new LiveEventCalendarDocument(remoteConfigKey ?? document.RemoteConfigKey,
                eventTypes ?? new List<LiveEventTypeDefinition>(document.EventTypes),
                recurringRules ?? new List<RecurringLiveEventRule>(document.RecurringRules),
                fixedEvents ?? new List<FixedLiveEventEntry>(document.FixedEvents),
                publishedStamps ?? new List<PublishedCalendarStamp>(document.PublishedStamps),
                ignoredWarnings ?? new List<IgnoredCalendarWarning>(document.IgnoredWarnings));
        }
    }
}
