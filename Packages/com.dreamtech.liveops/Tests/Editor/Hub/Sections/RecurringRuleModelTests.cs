using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEditor;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model thuần của màn Luật lặp (mục 3, [SD1 §4.1]): câu token, câu chu kỳ, bảng đợt kế tiếp, chỉ số mẫu, lỗi field và hai
    /// câu chú thích. Dữ liệu mẫu thiết kế đã đứng sẵn ở đúng tình huống khó nhất — nháp đổi tiền tố weekly-pass sang
    /// <c>pass-</c> trong khi <c>weekly-pass-35</c> đang chạy tới 14/9 — nên phần lớn test đọc thẳng từ đó.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class RecurringRuleModelTests
    {
        private const string WeeklyPassType = "weekly-pass";
        private const string SkyRaceType = "sky-race";
        private const string AnchorUtcText = "2026-01-05T00:00:00Z";
        private const int WeekHours = 168;
        private const int DayHours = 24;

        [SetUp]
        public void SetUp()
        {
            Undo.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
            Undo.ClearAll();
        }

        [Test]
        public void ActiveLongerThanPeriod_FieldError()
        {
            LiveOpsHubCalendarSession session = OpenSession(DocumentWith(SkyRaceType, "sky-race-", DayHours, 30));
            RecurringRuleModel model = Build(session, SkyRaceType);

            Assert.AreEqual(LiveOpsHubStrings.RecurringActiveLongerThanPeriodError, model.FieldErrorText(RecurringRuleFields.ActiveHours),
                "chạy 30 giờ trong chu kỳ 24 giờ: đợt sau mở trước khi đợt trước khép");
            Assert.IsFalse(model.IsValid, "luật này parser của game sẽ bỏ — form không được coi là hợp lệ");
            Assert.AreEqual(0, model.NextOccurrences.Count,
                "game bỏ hẳn luật chạy lâu hơn chu kỳ, nên bảng không được vẽ ra những đợt sẽ không bao giờ chạy");
            Assert.AreEqual(string.Empty, model.FieldErrorText(RecurringRuleFields.PeriodHours), "chu kỳ 24 giờ tự nó không sai");
        }

        [Test]
        public void Sentence_HasFourTokensBoundToFields()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            RecurringRuleModel model = Build(session, WeeklyPassType);

            List<string> tokenFields = new List<string>();
            List<string> tokenTexts = new List<string>();
            for (int index = 0; index < model.SentenceTokens.Count; index++)
            {
                RecurringSentenceToken token = model.SentenceTokens[index];
                if (!token.IsToken) continue;
                tokenFields.Add(token.FieldName);
                tokenTexts.Add(token.Text);
            }

            CollectionAssert.AreEqual(
                new[] { RecurringRuleFields.PeriodHours, RecurringRuleFields.Anchor, RecurringRuleFields.ActiveHours, RecurringRuleFields.IdPrefix },
                tokenFields, "câu đọc theo đúng thứ tự chu kỳ · neo · thời gian chạy · tiền tố ([SD1 §4.1])");
            Assert.AreEqual("7 ngày", tokenTexts[0], "168 giờ phải đọc thành 7 ngày, không in số giờ thô");
            StringAssert.Contains("5/1/2026", tokenTexts[1], "token neo có ngày đủ năm");
            StringAssert.Contains(LiveOpsHubStrings.Monday, tokenTexts[1], "token neo nói luôn thứ trong tuần");
            Assert.AreEqual("pass-", tokenTexts[3]);
        }

        [Test]
        public void Sentence_PrefixTokenWarnsWhileRunningIdIsChanged()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            RecurringRuleModel model = Build(session, WeeklyPassType);

            RecurringSentenceToken prefixToken = TokenOf(model, RecurringRuleFields.IdPrefix);
            Assert.AreEqual(RecurringTokenState.Warning, prefixToken.State,
                "nháp đã ghi đổi id weekly-pass-35 → pass-35, nên token tiền tố phải đang cảnh báo");
            Assert.IsTrue(prefixToken.IsMono, "tiền tố là chuỗi máy đọc — in mono");
            Assert.AreEqual(RecurringTokenState.Normal, TokenOf(model, RecurringRuleFields.PeriodHours).State);
        }

        [Test]
        public void NextOccurrences_RunningFirstAndOnlyRunningRowShowsOldId()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            RecurringRuleModel model = Build(session, WeeklyPassType);

            Assert.AreEqual(RecurringRuleModel.DefaultOccurrenceCount, model.NextOccurrences.Count);
            RecurringOccurrenceRow running = model.NextOccurrences[0];
            Assert.AreEqual(LiveEventPhase.Active, running.Phase, "hàng đầu là lần lặp đang chạy");
            Assert.AreEqual("pass-35", running.EventId);
            Assert.AreEqual("weekly-pass-35", running.PublishedEventId, "cột Id in cũ → mới cho đúng lần lặp người chơi đang giữ");
            Assert.IsTrue(running.IdChanges);
            Assert.AreEqual(new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc), running.StartUtc);
            Assert.AreEqual(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), running.EndUtc);
            Assert.AreEqual(LiveOpsHubStrings.RecurringPhaseRunning, running.NowText);

            RecurringOccurrenceRow next = model.NextOccurrences[1];
            Assert.AreEqual("pass-36", next.EventId);
            Assert.AreEqual(string.Empty, next.PublishedEventId, "đợt sắp tới chưa ai có gì để mất — in một id là đủ");
            Assert.AreEqual(LiveEventPhase.Upcoming, next.Phase);
            StringAssert.Contains(LiveOpsHubStrings.RecurringPhaseUpcoming, next.NowText);
        }

        [Test]
        public void NextOccurrences_CountIsClampedToMaximum()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            RecurringRuleModel model = RecurringRuleModel.Build(session, WeeklyPassType, RecurringPrefixDraft.None,
                RecurringRuleModel.MaximumOccurrenceCount + 20, FormatOf());

            Assert.AreEqual(RecurringRuleModel.MaximumOccurrenceCount, model.NextOccurrences.Count,
                "trần 50 đợt: quá số này bảng dài hơn màn hình mà không nói thêm gì");
        }

        [Test]
        public void CycleText_SeamlessAndWithRest()
        {
            LiveOpsHubCalendarSession seamless = OpenSession(DocumentWith(WeeklyPassType, "pass-", WeekHours, WeekHours));
            Assert.AreEqual("chạy 7 ngày · liền mạch, không nghỉ", Build(seamless, WeeklyPassType).CycleText);

            LiveOpsHubCalendarSession withRest = OpenSession(DocumentWith(SkyRaceType, "sky-race-", DayHours, 20));
            Assert.AreEqual("chạy 20 giờ | nghỉ 4 giờ", Build(withRest, SkyRaceType).CycleText);
        }

        [Test]
        public void AnchorNotice_OnlyWhenAnchorIsNotMidnight()
        {
            LiveOpsHubCalendarSession midnight = OpenSession(DocumentWith(WeeklyPassType, "pass-", WeekHours, WeekHours));
            Assert.AreEqual(string.Empty, Build(midnight, WeeklyPassType).AnchorNotice);

            LiveEventCalendarDocument shifted = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(WeeklyPassType, "Pass tuần", 1, false, string.Empty))
                .WithRecurringRule(new RecurringLiveEventRule(WeeklyPassType, "2026-01-05T09:30:00Z", "pass-", WeekHours, WeekHours, string.Empty))
                .Build();
            Assert.AreEqual(LiveOpsHubStrings.RecurringAnchorNotice, Build(OpenSession(shifted), WeeklyPassType).AnchorNotice);
        }

        [Test]
        public void AfterWriteNotice_StaysWhileOldOccurrenceIsStillRunning()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            RecurringRuleModel model = Build(session, WeeklyPassType);

            StringAssert.Contains("weekly-pass-35", model.AfterWriteNotice, "câu ở lại nói id người chơi đang giữ");
            StringAssert.Contains("pass-35", model.AfterWriteNotice, "và id mới sẽ thay nó");
            Assert.AreEqual("weekly-pass-", model.AfterWriteRevertPrefix, "nút Hoàn về trỏ đúng tiền tố của bản đã đăng");
        }

        [Test]
        public void AfterWriteNotice_EmptyWhenPrefixMatchesPublished()
        {
            LiveOpsHubCalendarSession session = OpenSession(DocumentWith(WeeklyPassType, "pass-", WeekHours, WeekHours));
            Assert.AreEqual(string.Empty, Build(session, WeeklyPassType).AfterWriteNotice,
                "không có bản so thì không có gì để nói là đã đổi");
        }

        [Test]
        public void PresetIndex_MatchesWeeklyBuiltInAndFallsBackToCustom()
        {
            LiveOpsHubCalendarSession weekly = OpenSession(DocumentWith(WeeklyPassType, "pass-", WeekHours, WeekHours));
            RecurringRuleModel weeklyModel = Build(weekly, WeeklyPassType);
            IReadOnlyList<LiveOpsRulePresetLibrary.Preset> presets = LiveOpsRulePresets.Resolve();
            Assert.GreaterOrEqual(weeklyModel.PresetIndex, 0, "luật đúng nhịp hằng tuần phải khớp một mẫu");
            Assert.AreEqual(WeekHours, presets[weeklyModel.PresetIndex].PeriodHours);
            Assert.AreEqual(WeekHours, presets[weeklyModel.PresetIndex].ActiveHours);

            LiveOpsHubCalendarSession custom = OpenSession(DocumentWith(WeeklyPassType, "pass-", 100, 50));
            Assert.AreEqual(LiveOpsRulePresets.CustomIndex, Build(custom, WeeklyPassType).PresetIndex,
                "sửa tay một field thì dropdown về Tùy chỉnh…");
        }

        [Test]
        public void Empty_WhenEventTypeHasNoRule()
        {
            LiveOpsHubCalendarSession session = OpenDesignSampleSession();
            RecurringRuleModel model = Build(session, "treasure-hunt");

            Assert.IsFalse(model.HasRule);
            Assert.AreEqual(0, model.SentenceTokens.Count);
            Assert.AreEqual(0, model.NextOccurrences.Count);
            Assert.AreEqual(string.Empty, model.CycleText);
        }

        private static RecurringSentenceToken TokenOf(RecurringRuleModel model, string fieldName)
        {
            for (int index = 0; index < model.SentenceTokens.Count; index++)
            {
                RecurringSentenceToken token = model.SentenceTokens[index];
                if (token.IsToken && string.Equals(token.FieldName, fieldName, StringComparison.Ordinal)) return token;
            }
            Assert.Fail("câu đọc thiếu token của field " + fieldName);
            return null;
        }

        private static RecurringRuleModel Build(LiveOpsHubCalendarSession session, string eventType)
        {
            return RecurringRuleModel.Build(session, eventType, RecurringPrefixDraft.None,
                RecurringRuleModel.DefaultOccurrenceCount, FormatOf());
        }

        private static LiveOpsHubFormat FormatOf()
        {
            return new LiveOpsHubFormat(LiveOpsDesignSample.DeviceOffset);
        }

        private static LiveEventCalendarDocument DocumentWith(string eventType, string idPrefix, int periodHours, int activeHours)
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition(eventType, eventType, 1, false, string.Empty))
                .WithRecurringRule(new RecurringLiveEventRule(eventType, AnchorUtcText, idPrefix, periodHours, activeHours, string.Empty))
                .Build();
        }

        private static LiveOpsHubCalendarSession OpenSession(LiveEventCalendarDocument document)
        {
            return LiveOpsHubTestServices.Build(LiveOpsHubTestServices.CreateBuilder(null)
                .WithCalendarAsset(LiveOpsHubTestServices.CreateMemoryAsset(document))).Session;
        }

        private static LiveOpsHubCalendarSession OpenDesignSampleSession()
        {
            return LiveOpsHubTestServices.FromDesignSample().Session;
        }
    }
}
