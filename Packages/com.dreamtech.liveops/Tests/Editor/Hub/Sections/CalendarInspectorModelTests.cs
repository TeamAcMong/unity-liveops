using System;
using NUnit.Framework;

using DreamTech.LiveOps.Tests;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Bốn trạng thái của inspector đợt [SD1 §3.10] và các quyết định của nó: tóm tắt ở (a), khoá mép đầu khi đang chạy,
    /// ánh xạ khoá thanh sinh từ luật về đúng luật, và (d) giữ NGUYÊN VĂN chuỗi giờ hỏng trong asset.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class CalendarInspectorModelTests
    {
        private static readonly DateTime WhileMidQuestRunningUtc = new DateTime(2026, 9, 18, 10, 0, 0, DateTimeKind.Utc);

        [TearDown]
        public void TearDown()
        {
            LiveOpsHubTestServices.ReleaseAll();
        }

        [Test]
        public void NothingSelected_SummaryCountsUnplaceable()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, string.Empty, services.Clock.UtcNow,
                services.Format);
            Assert.AreEqual(CalendarInspectorModel.StateNothingSelected, model.State);
            StringAssert.Contains("6", model.SummaryText, "lịch mẫu có 6 đợt cố định");
            StringAssert.Contains("2", model.SummaryText, "lịch mẫu có 2 luật lặp");
            StringAssert.Contains("1", model.SummaryText, "lava-quest-2026-10 không đặt được — vẫn được đếm, không biến mất");
        }

        [Test]
        public void FixedEvent_UpcomingHasDeleteWithoutDialog()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, LiveOpsDesignSample.HuntBonusEntryKey,
                services.Clock.UtcNow, services.Format);
            Assert.AreEqual(CalendarInspectorModel.StateFixedEvent, model.State);
            Assert.AreEqual("hunt-0916-bonus", model.EventId);
            Assert.AreEqual(LiveEventPhase.Upcoming, model.Phase);
            Assert.AreEqual(LiveOpsHubStrings.CalendarPhaseUpcoming, model.PhaseTagText);
            Assert.IsFalse(model.IsStartLocked, "đợt chưa bắt đầu: mép đầu không khoá");
            Assert.AreEqual(LiveOpsHubStrings.CalendarDeleteButton, model.DeleteButtonText);
            Assert.AreEqual(36, model.DurationHours, "16/9 12:00 → 18/9 00:00 là 36 giờ");
            Assert.IsTrue(model.HasOwnConfigKey, "hunt-0916-bonus tự khai hunt_bonus");
            StringAssert.Contains("hunt_default", model.ConfigKeyPlaceholder, "chữ dẫn nêu mặc định của loại treasure-hunt");
        }

        [Test]
        public void RunningEvent_StartLockedWithReason()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            ((ManualLiveOpsClock)services.Clock).Set(WhileMidQuestRunningUtc);
            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, LiveOpsDesignSample.LavaQuestMidEntryKey,
                WhileMidQuestRunningUtc, services.Format);
            Assert.AreEqual(LiveEventPhase.Active, model.Phase);
            Assert.IsTrue(model.IsStartLocked, "đang chạy: người chơi đã vào theo giờ bắt đầu cũ nên mép đầu khoá");
            Assert.IsNotEmpty(model.StartLockReason, "khoá thì phải nói vì sao — lý do luôn in thành chữ");
            Assert.AreEqual(LiveOpsHubStrings.CalendarDeleteButtonWithDialog, model.DeleteButtonText, "xoá đợt đang chạy sẽ hỏi");
        }

        /// <summary>
        /// Đợt ĐÃ KHÉP: khoá CẢ HAI mép. Bảng 7.0 cấm đổi giờ đợt đã khép (<c>LiveOpsConfirmationPolicy</c> trả <c>NotAllowed</c>)
        /// và <c>CalendarTimelinePresenter.ApplyEdit</c> bỏ lệnh KHÔNG một lời nào — nên chỗ duy nhất nói được cho người dùng là chính ô nhập.
        /// Trước lượt W8-UX2 hai ô giờ của đợt này vẫn bật và nuốt thao tác im lặng.
        /// </summary>
        [Test]
        public void EndedEvent_BothEdgesLockedWithReason()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, LiveOpsDesignSample.LavaQuestEarlyEntryKey,
                services.Clock.UtcNow, services.Format);
            Assert.AreEqual(LiveEventPhase.Ended, model.Phase, "lava-quest-2026-09a khép lúc 13/9 00:00, mốc mẫu là 13/9 01:47");
            Assert.IsTrue(model.IsStartLocked, "đã khép: mép đầu khoá");
            Assert.IsTrue(model.IsEndLocked, "đã khép: mép cuối khoá — không còn lôi dài hay rút ngắn được nữa");
            Assert.IsNotEmpty(model.StartLockReason, "khoá thì phải nói vì sao");
            Assert.IsNotEmpty(model.EndLockReason, "khoá thì phải nói vì sao");
        }

        [Test]
        public void RecurringBarKey_MapsToRule()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, "weekly-pass#35", services.Clock.UtcNow,
                services.Format);
            Assert.AreEqual(CalendarInspectorModel.StateRecurringEvent, model.State);
            Assert.AreEqual("weekly-pass", model.RuleEventType);
            StringAssert.Contains("weekly-pass", model.RecurringNoteText, "note chỉ đúng luật sinh ra đợt");
            Assert.AreEqual(string.Empty, CalendarInspectorModel.TypeIdOfBarKey(LiveOpsDesignSample.HuntBonusEntryKey),
                "khoá đợt cố định không có dấu ngăn — không được hiểu nhầm thành loại");
        }

        [Test]
        public void StripBarKey_IsNotAnEvent()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, "sky-race#strip#0",
                services.Clock.UtcNow, services.Format);
            Assert.AreEqual(CalendarInspectorModel.StateNothingSelected, model.State,
                "dải gom không ánh xạ ra một đợt (V-22 CC-TLMODEL-1) — cắt trước dấu ngăn sẽ ra tên loại và rơi nhầm vào (b)");
            Assert.IsTrue(CalendarInspectorModel.IsStripBarKey("sky-race#fixed-strip#entry-1"));
            Assert.IsFalse(CalendarInspectorModel.IsStripBarKey("weekly-pass#35"));
        }

        [Test]
        public void RecurringOccurrence_ShowsRealIdAndTimes()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarTimelinePresenter presenter = new CalendarTimelinePresenter(services);
            DateTime rangeStartUtc = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc);
            LiveOpsTimelineModel timelineModel = presenter.BuildModel(rangeStartUtc, rangeStartUtc.AddDays(21), 635f);
            LiveOpsTimelineBarModel bar = FindRecurringBar(timelineModel);
            Assert.IsNotNull(bar, "lịch mẫu có làn weekly-pass sinh từ luật");

            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, bar.BarKey, services.Clock.UtcNow,
                services.Format, bar);
            Assert.AreEqual(CalendarInspectorModel.StateRecurringEvent, model.State);
            Assert.AreEqual(bar.EventId, model.EventId,
                "pane-title in id lần lặp ('weekly-pass-35'), không in khoá nội bộ của timeline ('weekly-pass#35')");
            Assert.AreEqual(bar.StartUtc, model.OccurrenceStartUtc, "field Bắt đầu của (b) đọc giờ của chính lần lặp đang chọn");
            Assert.AreEqual(bar.EndUtc, model.OccurrenceEndUtc);
        }

        private static LiveOpsTimelineBarModel FindRecurringBar(LiveOpsTimelineModel model)
        {
            System.Collections.Generic.IReadOnlyList<LiveOpsTimelineLaneModel> lanes = model.Lanes;
            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                System.Collections.Generic.IReadOnlyList<LiveOpsTimelineBarModel> bars = lanes[laneIndex].Bars;
                for (int barIndex = 0; barIndex < bars.Count; barIndex++)
                {
                    LiveOpsTimelineBarModel bar = bars[barIndex];
                    if (!bar.IsStrip && bar.Source == LiveOpsTimelineBarSource.Recurring) return bar;
                }
            }
            return null;
        }

        [Test]
        public void UnreadableDate_KeptRawInAsset()
        {
            LiveOpsHubServices services = LiveOpsHubTestServices.FromDesignSample();
            CalendarInspectorModel model = CalendarInspectorModel.Build(services.Session, LiveOpsDesignSample.LavaQuestLateEntryKey,
                services.Clock.UtcNow, services.Format);
            Assert.AreEqual(CalendarInspectorModel.StateUnreadableTimes, model.State);
            Assert.AreEqual("2026-10-3", model.Entry.EndUtcText,
                "chuỗi không đọc được vẫn nằm nguyên văn trong asset — hub và bộ kiểm phải cùng thấy đợt hỏng");
            Assert.IsTrue(model.IsUnreadableEnd, "ô hỏng là Kết thúc, không phải Bắt đầu");
            Assert.IsNotEmpty(model.UnreadableFieldErrorText, "ô viền lỗi kèm câu nói dạng cần gõ");
            Assert.AreEqual("2026-10-03T00:00:00Z", model.UnreadableFixValueText, "nút sửa nhanh đề nghị đúng ngày đọc được");
            StringAssert.Contains("2026-10-03 00:00", model.UnreadableFixButtonText);
            Assert.AreEqual(LiveOpsHubStrings.CalendarPhaseUnplaceable, model.PhaseTagText);
        }
    }
}
