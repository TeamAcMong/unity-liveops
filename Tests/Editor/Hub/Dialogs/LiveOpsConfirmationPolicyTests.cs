using System;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Một test mỗi ô bảng 7.0 (PD-24). Tài liệu dựng tay (không dùng LiveOpsDesignSample: assembly test Hub chưa tham chiếu
    /// Support ở W1) nhưng giữ đúng mốc của mẫu thiết kế: "bây giờ" = 13/9/2026 08:47 UTC, luật weekly-pass neo 5/1/2026 chu kỳ
    /// 7 ngày nên lần đang chạy là weekly-pass-35 (7/9 → 14/9).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class LiveOpsConfirmationPolicyTests
    {
        private static readonly DateTime NowUtc = new DateTime(2026, 9, 13, 8, 47, 0, DateTimeKind.Utc);

        private const string RunningKey = "running-entry";
        private const string EndedKey = "ended-entry";
        private const string UpcomingKey = "upcoming-entry";
        private const string UnreadableKey = "unreadable-entry";
        private const string WeeklyPassType = "weekly-pass";
        private const string SkyRaceType = "sky-race";
        private const string TreasureHuntType = "treasure-hunt";
        private const string UnusedType = "star-tournament";

        private static FixedLiveEventEntry RunningEntry =>
            new FixedLiveEventEntry(RunningKey, "hunt-0912", TreasureHuntType, "2026-09-12T00:00:00Z", "2026-09-15T00:00:00Z", "");

        private static FixedLiveEventEntry EndedEntry =>
            new FixedLiveEventEntry(EndedKey, "lava-quest-2026-09a", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "");

        private static FixedLiveEventEntry UpcomingEntry =>
            new FixedLiveEventEntry(UpcomingKey, "lava-quest-2026-09b", "lava-quest", "2026-09-17T00:00:00Z", "2026-09-20T00:00:00Z", "");

        private static FixedLiveEventEntry UnreadableEntry =>
            new FixedLiveEventEntry(UnreadableKey, "lava-quest-2026-10", "lava-quest", "2026-10-01T00:00:00Z", "2026-10-3", "");

        private static RecurringLiveEventRule WeeklyPassRule =>
            new RecurringLiveEventRule(WeeklyPassType, "2026-01-05T00:00:00Z", "weekly-pass-", 168, 168, "");

        /// <summary>Chạy 00:00–01:00 mỗi ngày — lúc 08:47 không có lần nào đang chạy.</summary>
        private static RecurringLiveEventRule SkyRaceRule =>
            new RecurringLiveEventRule(SkyRaceType, "2026-01-01T00:00:00Z", "sky-race-", 24, 1, "");

        private static LiveEventCalendarDocument Draft()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(TreasureHuntType, "Săn kho báu", 4, false, "hunt_default"))
                .WithEventType(new LiveEventTypeDefinition(UnusedType, "Giải đấu", 7, true, ""))
                .WithRecurringRule(WeeklyPassRule)
                .WithRecurringRule(SkyRaceRule)
                .WithFixedEvent(RunningEntry)
                .WithFixedEvent(EndedEntry)
                .WithFixedEvent(UpcomingEntry)
                .WithFixedEvent(UnreadableEntry)
                .Build();
        }

        private static LiveEventCalendarDocument PublishedWith(params FixedLiveEventEntry[] entries)
        {
            var builder = new LiveEventCalendarDocumentBuilder();
            for (int index = 0; index < entries.Length; index++) builder.WithFixedEvent(entries[index]);
            return builder.Build();
        }

        private static LiveEventCalendarDocument PublishedWithRule(RecurringLiveEventRule rule)
        {
            return new LiveEventCalendarDocumentBuilder().WithRecurringRule(rule).Build();
        }

        private static LiveEventCalendarDocument DraftWithWeeklyPassPrefix(string idPrefix)
        {
            return Apply(Draft(), new SetRecurringRuleEdit(WeeklyPassRule.WithIdPrefix(idPrefix)));
        }

        private static LiveEventCalendarDocument Apply(LiveEventCalendarDocument document, LiveEventCalendarEdit edit)
        {
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(document, edit, out LiveEventCalendarDocument result), "lệnh sửa của fixture phải áp được");
            return result;
        }

        private static LiveEventCalendarDocument WithTimes(LiveEventCalendarDocument document, FixedLiveEventEntry entry, string startUtc, string endUtc)
        {
            return Apply(document, new ReplaceFixedEventEdit(entry.WithTimes(startUtc, endUtc)));
        }

        private static LiveOpsConfirmDecision Decide(LiveOpsEditOperation operation, LiveEventCalendarDocument after,
            LiveEventCalendarDocument published, string targetKey)
        {
            return LiveOpsConfirmationPolicy.Decide(operation, Draft(), after, published, NowUtc, targetKey);
        }

        private static void AssertRequirement(LiveOpsConfirmDecision decision, LiveOpsConfirmRequirement expected, string expectedReason)
        {
            Assert.AreEqual(expected, decision.Requirement, "mức");
            Assert.AreEqual(expectedReason, decision.ReasonCode, "mã lý do");
            if (expected != LiveOpsConfirmRequirement.TypeToConfirm) Assert.AreEqual(string.Empty, decision.TypeToConfirmText);
        }

        // ── Xoá đợt cố định ─────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void DeleteFixed_NotStartedUnpublished_NoConfirm()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.DeleteFixedEvent, null, null, UpcomingKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNotStartedUnpublished);
        }

        [Test]
        public void DeleteFixed_EndedEvent_NoConfirm()
        {
            // Q-14: đợt đã khép — kể cả đã đăng — xoá thẳng + toast Hoàn tác.
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.DeleteFixedEvent, null, PublishedWith(EndedEntry), EndedKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonEnded);
        }

        [Test]
        public void DeleteFixed_PublishedNotStarted_Level1()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.DeleteFixedEvent, null, PublishedWith(UpcomingEntry), UpcomingKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonPublishedNotStarted);
        }

        [Test]
        public void DeleteFixed_RunningInDraft_TypeToConfirmWithRunningId()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.DeleteFixedEvent, null, null, RunningKey);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);
            Assert.AreEqual(LiveOpsConfirmationPolicy.ReasonRunning, decision.ReasonCode);
            Assert.AreEqual("hunt-0912", decision.TypeToConfirmText);
            Assert.AreEqual("hunt-0912", decision.RunningEventId);
            Assert.AreEqual(new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), decision.RunningEndUtc);
        }

        [Test]
        public void DeleteFixed_UnreadableTimesUnpublished_NoConfirm()
        {
            // Giờ không đọc được = game không đặt được đợt = không đang chạy.
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.DeleteFixedEvent, null, null, UnreadableKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNotStartedUnpublished);
        }

        // ── Đổi giờ đợt cố định ─────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void ChangeTimes_NotStarted_None()
        {
            LiveEventCalendarDocument after = WithTimes(Draft(), UpcomingEntry, "2026-09-18T00:00:00Z", "2026-09-19T00:00:00Z");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeFixedEventTimes, after, PublishedWith(UpcomingEntry), UpcomingKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNotStarted);
        }

        [Test]
        public void ChangeTimes_ExtendRunning_None()
        {
            LiveEventCalendarDocument after = WithTimes(Draft(), RunningEntry, "2026-09-12T00:00:00Z", "2026-09-16T00:00:00Z");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeFixedEventTimes, after, null, RunningKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonExtendsRunning);
        }

        [Test]
        public void ChangeTimes_ShortenRunning_Level1()
        {
            LiveEventCalendarDocument after = WithTimes(Draft(), RunningEntry, "2026-09-12T00:00:00Z", "2026-09-14T00:00:00Z");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeFixedEventTimes, after, null, RunningKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonShortensRunning);
            Assert.AreEqual("hunt-0912", decision.RunningEventId);
        }

        [Test]
        public void ChangeTimes_ShortenRunningBeforeNow_Level1()
        {
            LiveEventCalendarDocument after = WithTimes(Draft(), RunningEntry, "2026-09-12T00:00:00Z", "2026-09-13T00:00:00Z");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeFixedEventTimes, after, null, RunningKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonShortensRunning);
        }

        [Test]
        public void ChangeTimes_EndedEvent_NotAllowed()
        {
            LiveEventCalendarDocument after = WithTimes(Draft(), EndedEntry, "2026-09-10T00:00:00Z", "2026-09-14T00:00:00Z");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeFixedEventTimes, after, null, EndedKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.NotAllowed, LiveOpsConfirmationPolicy.ReasonEnded);
        }

        [Test]
        public void ChangeTimes_RunningStartMoved_NotAllowedStartLocked()
        {
            LiveEventCalendarDocument after = WithTimes(Draft(), RunningEntry, "2026-09-11T00:00:00Z", "2026-09-15T00:00:00Z");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeFixedEventTimes, after, null, RunningKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.NotAllowed, LiveOpsConfirmationPolicy.ReasonRunningStartLocked);
        }

        // ── Đổi id / loại đợt cố định ───────────────────────────────────────────────────────────────────────────────

        [Test]
        public void RenameFixed_NotStarted_None()
        {
            LiveEventCalendarDocument after = Apply(Draft(), new ReplaceFixedEventEdit(UpcomingEntry.WithEventId("lava-quest-2026-09c")));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RenameOrRetypeFixedEvent, after, null, UpcomingKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNotStarted);
        }

        [Test]
        public void RenameFixed_EndedEvent_None()
        {
            LiveEventCalendarDocument after = Apply(Draft(), new ReplaceFixedEventEdit(EndedEntry.WithEventId("lava-quest-old")));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RenameOrRetypeFixedEvent, after, null, EndedKey);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonEnded);
        }

        [Test]
        public void RenameFixed_Running_TypeToConfirmOldId()
        {
            LiveEventCalendarDocument after = Apply(Draft(), new ReplaceFixedEventEdit(RunningEntry.WithEventType("lava-quest")));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RenameOrRetypeFixedEvent, after, null, RunningKey);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);
            Assert.AreEqual("hunt-0912", decision.TypeToConfirmText);
        }

        // ── Đổi tiền tố / neo / chu kỳ của luật ─────────────────────────────────────────────────────────────────────

        [Test]
        public void RecurringPrefix_NoRunningOccurrence_None()
        {
            LiveEventCalendarDocument after = Apply(Draft(), new SetRecurringRuleEdit(SkyRaceRule.WithIdPrefix("race-")));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeRecurringIdentity, after, null, SkyRaceType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNoRunningOccurrence);
        }

        [Test]
        public void RecurringPrefix_RunningOccurrenceChangesId_TypeToConfirm()
        {
            LiveEventCalendarDocument after = Apply(Draft(), new SetRecurringRuleEdit(WeeklyPassRule.WithIdPrefix("pass-")));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeRecurringIdentity, after, null, WeeklyPassType);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);
            Assert.AreEqual(LiveOpsConfirmationPolicy.ReasonRunning, decision.ReasonCode);
            Assert.AreEqual("weekly-pass-35", decision.TypeToConfirmText);
            Assert.AreEqual(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), decision.RunningEndUtc);
        }

        [Test]
        public void RecurringAnchor_RunningOccurrenceKeepsId_None()
        {
            // Neo tiến một ngày, chu kỳ giữ nguyên: 13/9 08:47 vẫn rơi vào lần số 35 (8/9 → 15/9) nên id người chơi không đổi.
            LiveEventCalendarDocument after = Apply(Draft(), new SetRecurringRuleEdit(WeeklyPassRule.WithAnchor("2026-01-06T00:00:00Z")));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeRecurringIdentity, after, null, WeeklyPassType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonRunningIdKept);
        }

        [Test]
        public void RecurringPrefix_RevertToPublished_None()
        {
            // Nháp đã ghi tiền tố pass-; bản đã đăng dùng weekly-pass-. Hoàn về weekly-pass- trả lại đúng id người chơi đang
            // giữ (7.4 "so bản so nếu có") — không ai mất gì, không bắt gõ.
            LiveEventCalendarDocument before = DraftWithWeeklyPassPrefix("pass-");
            LiveEventCalendarDocument after = Draft();
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeRecurringIdentity, before, after,
                PublishedWithRule(WeeklyPassRule), NowUtc, WeeklyPassType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonRunningIdKept);
            Assert.AreEqual("weekly-pass-35", decision.RunningEventId);
        }

        [Test]
        public void RecurringPrefix_WithBaseline_TypesPublishedRunningId()
        {
            // Nháp pass- → p-: người chơi vẫn đang giữ weekly-pass-35 của bản đã đăng — chữ phải gõ là id đó, không phải pass-35.
            LiveEventCalendarDocument before = DraftWithWeeklyPassPrefix("pass-");
            LiveEventCalendarDocument after = DraftWithWeeklyPassPrefix("p-");
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeRecurringIdentity, before, after,
                PublishedWithRule(WeeklyPassRule), NowUtc, WeeklyPassType);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);
            Assert.AreEqual(LiveOpsConfirmationPolicy.ReasonRunning, decision.ReasonCode);
            Assert.AreEqual("weekly-pass-35", decision.TypeToConfirmText);
            Assert.AreEqual(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), decision.RunningEndUtc);
        }

        [Test]
        public void RecurringPrefix_BaselineWithoutRule_UsesDraftBefore()
        {
            // Bản so có nhưng không có luật weekly-pass: quay về nháp trước khi sửa (hỏi thừa an toàn hơn bỏ sót).
            LiveEventCalendarDocument after = DraftWithWeeklyPassPrefix("pass-");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeRecurringIdentity, after, PublishedWith(EndedEntry), WeeklyPassType);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);
            Assert.AreEqual("weekly-pass-35", decision.TypeToConfirmText);
        }

        // ── Đổi thời gian chạy của luật ─────────────────────────────────────────────────────────────────────────────

        [Test]
        public void RecurringActiveHours_NoRunningOccurrence_None()
        {
            LiveEventCalendarDocument after = Apply(Draft(), new SetRecurringRuleEdit(SkyRaceRule.WithActiveHours(2)));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeRecurringActiveHours, after, null, SkyRaceType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNoRunningOccurrence);
        }

        [Test]
        public void RecurringActiveHours_ExtendRunning_None()
        {
            // Lần 35 bắt đầu 7/9 00:00: chạy 160 giờ → khép 13/9 16:00 (đang chạy lúc 08:47); 165 giờ là kéo dài.
            LiveEventCalendarDocument before = Apply(Draft(), new SetRecurringRuleEdit(WeeklyPassRule.WithActiveHours(160)));
            LiveEventCalendarDocument after = Apply(Draft(), new SetRecurringRuleEdit(WeeklyPassRule.WithActiveHours(165)));
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeRecurringActiveHours, before, after,
                null, NowUtc, WeeklyPassType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonExtendsRunning);
        }

        [Test]
        public void RecurringActiveHours_ShortenRunning_Level1()
        {
            LiveEventCalendarDocument after = Apply(Draft(), new SetRecurringRuleEdit(WeeklyPassRule.WithActiveHours(150)));
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.ChangeRecurringActiveHours, after, null, WeeklyPassType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonShortensRunning);
            Assert.AreEqual("weekly-pass-35", decision.RunningEventId);
        }

        // ── Xoá luật lặp ────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void RemoveRule_NoRunningOccurrence_None()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RemoveRecurringRule, null, null, SkyRaceType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNoRunningOccurrence);
        }

        [Test]
        public void RemoveRule_RunningOccurrence_TypeToConfirmOccurrenceId()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RemoveRecurringRule, null, null, WeeklyPassType);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);
            Assert.AreEqual("weekly-pass-35", decision.TypeToConfirmText);
        }

        [Test]
        public void RemoveRule_WithBaseline_TypesPublishedRunningId()
        {
            LiveEventCalendarDocument before = DraftWithWeeklyPassPrefix("pass-");
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.RemoveRecurringRule, before, null,
                PublishedWithRule(WeeklyPassRule), NowUtc, WeeklyPassType);
            Assert.AreEqual(LiveOpsConfirmRequirement.TypeToConfirm, decision.Requirement);
            Assert.AreEqual("weekly-pass-35", decision.TypeToConfirmText);
        }

        [Test]
        public void RemoveRule_BaselineRuleNotRunning_None()
        {
            // Bản đã đăng không có lần lặp nào đang chạy (chạy 1 giờ mỗi ngày, lúc 08:47 đã khép) — người chơi không giữ id nào.
            RecurringLiveEventRule publishedNotRunning = new RecurringLiveEventRule(WeeklyPassType, "2026-01-05T00:00:00Z", "weekly-pass-", 24, 1, "");
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RemoveRecurringRule, null, PublishedWithRule(publishedNotRunning), WeeklyPassType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNoRunningOccurrence);
        }

        // ── Loại ────────────────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void RemoveType_Unused_None()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RemoveEventType, null, null, UnusedType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonTypeUnused);
            Assert.AreEqual(0, decision.InUseCount);
        }

        [Test]
        public void RemoveType_InUse_NotAllowedWithCount()
        {
            // lava-quest: 3 đợt cố định (09a, 09b, 10), không có luật.
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RemoveEventType, null, null, "lava-quest");
            AssertRequirement(decision, LiveOpsConfirmRequirement.NotAllowed, LiveOpsConfirmationPolicy.ReasonTypeInUse);
            Assert.AreEqual(3, decision.InUseCount);
        }

        [Test]
        public void RemoveType_UsedOnlyByRule_NotAllowedCountsRule()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.RemoveEventType, null, null, WeeklyPassType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.NotAllowed, LiveOpsConfirmationPolicy.ReasonTypeInUse);
            Assert.AreEqual(1, decision.InUseCount);
        }

        [Test]
        public void EditTypeFields_Always_None()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.EditEventTypeFields, null, null, TreasureHuntType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonAlways);
        }

        // ── Thay nháp · Khôi phục · Gỡ dấu · Ghi đè đĩa (cấp 1 luôn) ────────────────────────────────────────────────

        [Test]
        public void ReplaceDraft_Always_Level1()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.ReplaceDraftWithJson, null, null, null),
                LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonAlways);
        }

        [Test]
        public void RestorePublished_Always_Level1()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.RestorePublishedIntoDraft, null, null, null),
                LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonAlways);
        }

        [Test]
        public void RemovePublishedStamp_Always_Level1()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.RemovePublishedStamp, null, null, null),
                LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonAlways);
        }

        [Test]
        public void OverwriteDiskChanges_Always_Level1()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.OverwriteDiskChanges, null, null, null),
                LiveOpsConfirmRequirement.Level1, LiveOpsConfirmationPolicy.ReasonAlways);
        }

        [Test]
        public void MarkPublished_DedicatedDialog()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.MarkPublished, null, null, null),
                LiveOpsConfirmRequirement.DedicatedDialog, LiveOpsConfirmationPolicy.ReasonAlways);
        }

        // ── Có xem trước / popover: không hỏi ───────────────────────────────────────────────────────────────────────

        [Test]
        public void ApplySafeRepair_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.ApplySafeRepair, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonPreviewed);
        }

        [Test]
        public void ApplyProposal_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.ApplyProposal, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonPreviewed);
        }

        [Test]
        public void IgnoreWarning_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.IgnoreWarning, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonPreviewed);
        }

        [Test]
        public void AddFixedEvent_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.AddFixedEvent, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonPreviewed);
        }

        [Test]
        public void AddRecurringRule_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.AddRecurringRule, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonPreviewed);
        }

        // ── V-12, V-15, V-14 ────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void ReorderEventTypes_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.ReorderEventTypes, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNothingToLose);
        }

        [Test]
        public void RemoveIgnoredWarning_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.RemoveIgnoredWarning, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNothingToLose);
        }

        [Test]
        public void ImportRunningJsonIntoNewAsset_Always_None()
        {
            AssertRequirement(Decide(LiveOpsEditOperation.ImportRunningJsonIntoNewAsset, null, null, null),
                LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNothingToLose);
        }

        // ── Hợp đồng chung ──────────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void EveryOperation_DecidesWithoutThrow_AndHasLabel()
        {
            LiveEventCalendarDocument draft = Draft();
            foreach (LiveOpsEditOperation operation in Enum.GetValues(typeof(LiveOpsEditOperation)))
            {
                LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(operation, draft, draft, null, NowUtc, RunningKey);
                Assert.IsNotNull(decision, operation.ToString());
                Assert.IsNotEmpty(decision.ReasonCode, operation.ToString());
                Assert.IsNotEmpty(LiveOpsConfirmationPolicy.LabelOf(decision.Requirement), operation.ToString());
            }
        }

        [Test]
        public void MissingTarget_None_NeverThrows()
        {
            LiveOpsConfirmDecision decision = Decide(LiveOpsEditOperation.DeleteFixedEvent, null, null, "no-such-key");
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonTargetMissing);
        }

        [Test]
        public void BrokenRecurringRule_TreatedAsNotRunning_NeverThrows()
        {
            LiveEventCalendarDocument broken = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule(WeeklyPassType, "2026-01-05T00:00:00Z", "weekly#pass-", 168, 168, ""))
                .Build();
            LiveOpsConfirmDecision decision = LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.RemoveRecurringRule, broken, null, null,
                NowUtc, WeeklyPassType);
            AssertRequirement(decision, LiveOpsConfirmRequirement.None, LiveOpsConfirmationPolicy.ReasonNoRunningOccurrence);
        }

        [Test]
        public void ChangeOperation_WithoutDraftAfter_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                LiveOpsConfirmationPolicy.Decide(LiveOpsEditOperation.ChangeFixedEventTimes, Draft(), null, null, NowUtc, RunningKey));
        }
    }
}
