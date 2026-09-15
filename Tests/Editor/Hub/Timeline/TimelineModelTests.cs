using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using DreamTech.LiveOps.Unity;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Model timeline trên dữ liệu mẫu (Logic, <c>-nographics</c>): hàng phụ + vùng chồng treasure-hunt, chip "Không đặt được"/"Đợt tới",
    /// meta/chip header làn, dải gom sky-race ở zoom Tháng, luật hằng giờ không bị cắt ở 512, minimap, làn ẩn, D-5 (ChangedTooltip rỗng)
    /// và họ intent đóng (V-10).
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class TimelineModelTests
    {
        private const float DesignTrackWidth = 635f;

        private static DateTime Utc(int month, int day, int hour = 0, int minute = 0)
        {
            return new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        [Test]
        public void Model_DesignSample_TreasureHuntTwoRowsAndOverlap()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).Build();

            CollectionAssert.AreEqual(new[] { "lava-quest", "sky-race", "star-tournament", "treasure-hunt", "weekly-pass" },
                TimelineTestQueries.Map(model.Lanes, lane => lane.TypeId).ToArray(), "thứ tự làn = thứ tự loại trong tài liệu");

            LiveOpsTimelineLaneModel treasureHunt = model.FindLane("treasure-hunt");
            Assert.AreEqual(2, treasureHunt.RowCount, "treasure-hunt có 2 hàng phụ");
            Assert.AreEqual(7, treasureHunt.ColorSlot);
            Assert.IsFalse(treasureHunt.IsRecurring);
            LiveOpsTimelineBarModel huntEarly = TimelineTestQueries.Single(treasureHunt.Bars, bar => bar.EventId == "hunt-0914");
            LiveOpsTimelineBarModel huntBonus = TimelineTestQueries.Single(treasureHunt.Bars, bar => bar.EventId == "hunt-0916-bonus");
            Assert.AreEqual(0, huntEarly.RowIndex, "hunt-0914 hàng 0");
            Assert.AreEqual(1, huntBonus.RowIndex, "hunt-0916-bonus hàng 1");
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, huntBonus.BarKey, "thanh cố định khoá theo EntryKey");
            Assert.AreEqual(LiveOpsTimelineBarSource.Fixed, huntBonus.Source);
            Assert.IsTrue(huntBonus.IsDropped, "game bỏ đợt chồng giờ bắt đầu muộn hơn");
            Assert.IsFalse(huntEarly.IsDropped);
            Assert.AreEqual(HealthState.Blocked, huntBonus.WorstFinding);
            Assert.AreEqual(HealthState.Warning, huntEarly.WorstFinding, "hunt-0914 Nên xem (không tự khai configKey)");
            Assert.AreEqual(LiveEventPhase.Upcoming, huntBonus.Phase);
            Assert.IsTrue(huntBonus.IsChangedSincePublished, "hunt-0916-bonus chưa có trong bản đã đăng");

            Assert.AreEqual(1, treasureHunt.OverlapRanges.Count, "một vùng chồng phủ cả hai hàng phụ");
            Assert.AreEqual(Utc(9, 16, 12), treasureHunt.OverlapRanges[0].startUtc);
            Assert.AreEqual(Utc(9, 17), treasureHunt.OverlapRanges[0].endUtc);
            Assert.AreEqual("cố định · 2 đợt", treasureHunt.MetaText);
            Assert.AreEqual("Chồng giờ: giữ đợt sớm hơn", treasureHunt.SecondaryMetaText);
            Assert.AreEqual(HealthState.Blocked, treasureHunt.ChipState, "chip ● 1");
            Assert.AreEqual(1, treasureHunt.ChipCount);
            Assert.AreEqual(0, treasureHunt.UnplaceableCount);
            Assert.IsNull(treasureHunt.NextOutsideRange, "làn có thanh trong khoảng thì không có chip Đợt tới");

            LiveOpsTimelineLaneModel weeklyPass = model.FindLane("weekly-pass");
            Assert.IsTrue(weeklyPass.IsRecurring);
            Assert.AreEqual(1, weeklyPass.RowCount);
            Assert.AreEqual("lặp mỗi 7 ngày · chạy 7 ngày", weeklyPass.MetaText);
            Assert.AreEqual(string.Empty, weeklyPass.SecondaryMetaText);
            Assert.AreEqual(HealthState.Warning, weeklyPass.ChipState, "chip ◆ 1 — mất tiến độ vì đổi tiền tố");
            Assert.AreEqual(1, weeklyPass.ChipCount);
            LiveOpsTimelineBarModel runningPass = TimelineTestQueries.Single(weeklyPass.Bars, bar => bar.EventId == "pass-35");
            Assert.AreEqual("weekly-pass#35", runningPass.BarKey);
            Assert.AreEqual(LiveOpsTimelineBarSource.Recurring, runningPass.Source);
            Assert.AreEqual("weekly-pass-35", runningPass.RenamedFromId, "nhãn \"weekly-pass-35 → pass-35\"");
            Assert.IsTrue(runningPass.IsChangedSincePublished, "vuông --changed trên đợt đang chạy bị đổi id");
            Assert.IsTrue(runningPass.IsRunning);
            Assert.AreEqual(HealthState.Warning, runningPass.WorstFinding);
            LiveOpsTimelineBarModel nextPass = TimelineTestQueries.Single(weeklyPass.Bars, bar => bar.EventId == "pass-36");
            Assert.AreEqual(string.Empty, nextPass.RenamedFromId);
            Assert.AreEqual(HealthState.Ok, nextPass.WorstFinding);

            LiveOpsTimelineLaneModel skyRace = model.FindLane("sky-race");
            Assert.AreEqual("lặp mỗi 24 giờ · chạy 20 giờ", skyRace.MetaText);
            Assert.AreEqual(HealthState.Ok, skyRace.ChipState);
            Assert.AreEqual(0, skyRace.ChipCount);
            Assert.IsTrue(TimelineTestQueries.Single(skyRace.Bars, bar => bar.EventId == "sky-race-251").IsRunning, "sky-race-251 đang chạy lúc 08:47");

            LiveOpsTimelineLaneModel lavaQuest = model.FindLane("lava-quest");
            Assert.AreEqual(HealthState.Ok, lavaQuest.ChipState,
                "lava-quest không chip: đợt bị bỏ duy nhất đã có chip Không đặt được, khoảng trống 11 ngày chỉ là Nên xem");
            LiveOpsTimelineBarModel endedLava = TimelineTestQueries.Single(lavaQuest.Bars, bar => bar.EventId == "lava-quest-2026-09a");
            Assert.IsTrue(endedLava.IsEnded, "09a đã khép (--ended)");
            Assert.AreEqual(LiveEventPhase.Ended, endedLava.Phase);
            Assert.IsFalse(endedLava.IsChangedSincePublished);
            Assert.IsTrue(TimelineTestQueries.Single(lavaQuest.Bars, bar => bar.EventId == "lava-quest-2026-09b").IsChangedSincePublished, "09b đổi kết thúc 19/9 → 20/9");

            Assert.AreEqual(Utc(9, 11, 16, 20), model.PublishedAtUtc, "cờ đã đăng 11/9 16:20");
            Assert.AreEqual(LiveOpsDesignSample.PublishedSha256Hex.Substring(0, 6), model.PublishedShortSha);
            Assert.AreEqual(LiveOpsDesignSample.NowUtc, model.NowUtc);

            foreach (LiveOpsTimelineLaneModel lane in model.Lanes)
            {
                Assert.LessOrEqual(LiveOpsTimelineVertexBudget.EstimateLane(lane, model.Geometry), LiveOpsTimelineVertexBudget.LaneSafetyBudget, lane.TypeId);
            }
        }

        [Test]
        public void Model_ChangedTooltip_AlwaysEmpty_PresenterFillsIt()
        {
            // V-21 D-5: model đặt IsChangedSincePublished nhưng không dựng câu "Khác bản đã đăng: …" (việc của LiveOpsChangeText ở W4).
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).Build();
            int changedCount = 0;
            foreach (LiveOpsTimelineLaneModel lane in model.Lanes)
            {
                foreach (LiveOpsTimelineBarModel bar in lane.Bars)
                {
                    Assert.AreEqual(string.Empty, bar.ChangedTooltip, bar.EventId);
                    if (bar.IsChangedSincePublished) changedCount++;
                }
            }
            Assert.AreEqual(4, changedCount, "pass-35, hunt-0914, hunt-0916-bonus, lava-quest-2026-09b");

            LiveOpsTimelineModel withoutDiff = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).WithPublishedDiff(null).WithCheckReport(null).Build();
            Assert.IsFalse(TimelineTestQueries.Any(TimelineTestQueries.AllBars(withoutDiff), bar => bar.IsChangedSincePublished), "chưa đăng thì không có vuông");
            Assert.IsTrue(TimelineTestQueries.All(TimelineTestQueries.AllBars(withoutDiff), bar => bar.WorstFinding == HealthState.Ok), "chưa kiểm thì không tô lỗi");
            Assert.IsTrue(TimelineTestQueries.Single(withoutDiff.FindLane("treasure-hunt").Bars, bar => bar.EventId == "hunt-0916-bonus").IsDropped,
                "bị bỏ đọc từ bản biên dịch, không cần báo cáo kiểm");
        }

        [Test]
        public void Model_UnplaceableChip()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).Build();
            LiveOpsTimelineLaneModel lavaQuest = model.FindLane("lava-quest");
            Assert.AreEqual(1, lavaQuest.UnplaceableCount, "chip \"Không đặt được (1)\": lava-quest-2026-10 kết thúc \"2026-10-3\"");
            Assert.AreEqual("cố định", lavaQuest.MetaText, "chip nằm trong dòng meta nên meta chỉ còn \"cố định\"");
            Assert.AreEqual(2, lavaQuest.Bars.Count, "09a và 09b; đợt không đặt được không có thanh nhưng không biến mất");

            // Đợt không đặt được vẫn không có thanh dù khoảng chứa giờ bắt đầu đọc được của nó.
            LiveOpsTimelineModel october = TimelineDesignSampleInput.Build(Utc(9, 28), LiveOpsTimelineZoom.ThreeWeeks, DesignTrackWidth).Build();
            Assert.AreEqual(0, TimelineTestQueries.Count(october.FindLane("lava-quest").Bars, bar => bar.EventId == "lava-quest-2026-10"));
            Assert.AreEqual(1, october.FindLane("lava-quest").UnplaceableCount);

            LiveEventCalendarDocument repaired = TimelineDesignSampleInput.WithFixedEnd(LiveOpsDesignSample.Document, LiveOpsDesignSample.LavaQuestLateEntryKey,
                "2026-10-03T00:00:00Z");
            LiveOpsTimelineLaneModel repairedLane = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).WithDocument(repaired).WithCompilation(null)
                .WithCheckReport(null).Build().FindLane("lava-quest");
            Assert.AreEqual(0, repairedLane.UnplaceableCount);
            Assert.AreEqual("cố định · 3 đợt", repairedLane.MetaText);
        }

        [Test]
        public void Model_NextOutsideRangeChip()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).Build();
            LiveOpsTimelineLaneModel starTournament = model.FindLane("star-tournament");
            Assert.AreEqual(0, starTournament.Bars.Count);
            Assert.IsNotNull(starTournament.NextOutsideRange, "chip \"Đợt tới: star-tournament-2026-10 · 3/10 00:00 · còn 19 ngày 15 giờ ›\"");
            Assert.AreEqual("star-tournament-2026-10", starTournament.NextOutsideRange.EventId);
            Assert.AreEqual(Utc(10, 3), starTournament.NextOutsideRange.StartUtc);
            Assert.AreEqual(LiveOpsDesignSample.StarTournamentEntryKey, starTournament.NextOutsideRange.BarKey, "bấm chip căn khung theo EntryKey");
            Assert.AreEqual("cố định · 1 đợt, ngoài khung", starTournament.MetaText);

            LiveOpsTimelineModel october = TimelineDesignSampleInput.Build(Utc(9, 28), LiveOpsTimelineZoom.ThreeWeeks, DesignTrackWidth).Build();
            LiveOpsTimelineLaneModel octoberStar = october.FindLane("star-tournament");
            Assert.AreEqual(1, octoberStar.Bars.Count);
            Assert.IsNull(octoberStar.NextOutsideRange, "làn có thanh trong khoảng thì không có chip");
            Assert.AreEqual("cố định · 1 đợt", octoberStar.MetaText);

            LiveOpsTimelineModel afterEverything = TimelineDesignSampleInput.Build(new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                LiveOpsTimelineZoom.ThreeWeeks, DesignTrackWidth).Build();
            Assert.IsNull(afterEverything.FindLane("star-tournament").NextOutsideRange, "không còn đợt phía sau thì không có chip");
        }

        [Test]
        public void Model_MonthZoom_SkyRaceStrip42()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.Build(Utc(9, 8), LiveOpsTimelineZoom.Month, DesignTrackWidth).Build();
            Assert.AreEqual(0.63, model.Geometry.PixelsPerHour, 0.005);

            LiveOpsTimelineLaneModel skyRace = model.FindLane("sky-race");
            Assert.AreEqual(1, skyRace.Bars.Count, "khe nghỉ 4 giờ × 0,63 = 2,5px < 3px: gom thành một dải");
            LiveOpsTimelineBarModel strip = skyRace.Bars[0];
            Assert.AreEqual(LiveOpsTimelineBarSource.RecurringStrip, strip.Source);
            Assert.IsTrue(strip.IsStrip);
            Assert.AreEqual(42, strip.StripCount, "\"sky-race · 42 đợt · 20 giờ/ngày\"");
            Assert.AreEqual("sky-race-246…287", strip.EventId);
            Assert.AreEqual("sky-race#strip#246", strip.BarKey);
            Assert.AreEqual(Utc(9, 8), strip.StartUtc);
            Assert.AreEqual(Utc(10, 19, 20), strip.EndUtc);
            Assert.IsTrue(strip.IsRunning, "sky-race-251 đang chạy nằm trong dải");
            Assert.IsFalse(strip.IsEnded);

            LiveOpsTimelineLaneModel weeklyPass = model.FindLane("weekly-pass");
            CollectionAssert.AreEqual(new[] { "pass-35", "pass-36", "pass-37", "pass-38", "pass-39", "pass-40", "pass-41" },
                TimelineTestQueries.Map(weeklyPass.Bars, bar => bar.EventId).ToArray(), "pass-35…41 vẫn từng thanh có nhãn số (liền nhau nhưng rộng 106px)");
            Assert.IsTrue(TimelineTestQueries.All(weeklyPass.Bars, bar => bar.Source == LiveOpsTimelineBarSource.Recurring));

            LiveOpsTimelineModel threeWeeks = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).Build();
            Assert.IsTrue(TimelineTestQueries.All(threeWeeks.FindLane("sky-race").Bars, bar => bar.Source == LiveOpsTimelineBarSource.Recurring && bar.StripCount == 1),
                "ở 3 tuần khe 5px: vẽ từng thanh");

            Assert.Less(LiveOpsTimelineVertexBudget.EstimateLane(skyRace, model.Geometry), LiveOpsTimelineVertexBudget.EstimateLane(
                threeWeeks.FindLane("sky-race"), threeWeeks.Geometry), "một element thay 42 thanh — giữ ngân sách vertex");
        }

        [Test]
        public void Model_HourlyRule_NotTruncatedAt512()
        {
            LiveEventCalendarDocument document = TimelineDesignSampleInput.WithHourlyRule(LiveOpsDesignSample.Document);
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);
            RecurringLiveEventCalendar hourlyCalendar = TimelineTestQueries.Single(compilation.RecurringCalendars, calendar => calendar.EventType == TimelineDesignSampleInput.HourlyType);
            Assert.AreEqual(RecurringLiveEventCalendar.MaximumInstancesPerQuery,
                hourlyCalendar.GetInstances(TimelineDesignSampleInput.HourlyType, Utc(9, 8), Utc(10, 20)).Count,
                "tiền đề: truy vấn thẳng của lịch lặp bị cắt ở 512 — model không được dùng nó");

            LiveOpsTimelineModel month = TimelineDesignSampleInput.Build(Utc(9, 8), LiveOpsTimelineZoom.Month, DesignTrackWidth)
                .WithDocument(document).WithCompilation(compilation).WithCheckReport(null).WithPublishedDiff(null).Build();
            LiveOpsTimelineLaneModel hourly = month.FindLane(TimelineDesignSampleInput.HourlyType);
            Assert.AreEqual(42 * 24, TimelineTestQueries.Sum(hourly.Bars, bar => bar.StripCount), "đủ 1.008 lần lặp trong 42 ngày, không dừng ở 512");
            Assert.AreEqual(Utc(9, 8), hourly.Bars[0].StartUtc);
            Assert.AreEqual(Utc(10, 20), hourly.Bars[hourly.Bars.Count - 1].EndUtc, "lần lặp cuối kết thúc đúng cuối khoảng — nửa sau khung không bị bỏ trống");
            Assert.LessOrEqual(LiveOpsTimelineVertexBudget.EstimateLane(hourly, month.Geometry), LiveOpsTimelineVertexBudget.LaneSafetyBudget);
            Assert.AreEqual("lặp mỗi 1 giờ · chạy 1 giờ", hourly.MetaText);

            LiveOpsTimelineModel day = TimelineDesignSampleInput.Build(Utc(9, 13), LiveOpsTimelineZoom.Day, DesignTrackWidth)
                .WithDocument(document).WithCompilation(compilation).WithCheckReport(null).WithPublishedDiff(null).Build();
            LiveOpsTimelineLaneModel hourlyDay = day.FindLane(TimelineDesignSampleInput.HourlyType);
            Assert.AreEqual(24, hourlyDay.Bars.Count, "zoom Ngày: mỗi giờ rộng 26px, đủ nhãn — không gom");
            Assert.IsTrue(TimelineTestQueries.All(hourlyDay.Bars, bar => bar.Source == LiveOpsTimelineBarSource.Recurring));

            // Minimap (khoảng ~60 ngày) cũng không bị cắt: đoạn cuối của dải hằng giờ chạm cuối khoảng minimap.
            LiveOpsTimelineMinimapBand hourlyBand = TimelineTestQueries.Single(month.Minimap.Bands, band => band.TypeId == TimelineDesignSampleInput.HourlyType);
            Assert.AreEqual(month.Minimap.RangeEndUtc, hourlyBand.Segments[hourlyBand.Segments.Count - 1].endUtc);
        }

        [Test]
        public void Model_Minimap_DesignSampleRangeAndMarks()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).Build();
            LiveOpsTimelineMinimapModel minimap = model.Minimap;

            Assert.AreEqual(Utc(8, 14), minimap.RangeStartUtc, "30 ngày trước bây giờ");
            Assert.AreEqual(Utc(10, 13), minimap.RangeEndUtc, "7 ngày sau đợt cố định cuối (star-tournament kết thúc 6/10)");
            Assert.AreEqual(Utc(9, 8), minimap.ViewportStartUtc);
            Assert.AreEqual(Utc(9, 29), minimap.ViewportEndUtc);
            Assert.AreEqual(Utc(9, 11, 16, 20), minimap.PublishedAtUtc);
            Assert.AreEqual(5, minimap.Bands.Count, "một dải mỗi làn");
            Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, TimelineTestQueries.Map(minimap.Bands, band => band.RowIndex).ToArray());
            Assert.AreEqual(1, TimelineTestQueries.Single(minimap.Bands, band => band.TypeId == "weekly-pass").Segments.Count, "weekly-pass liền nhau: một đoạn");
            Assert.AreEqual(2, TimelineTestQueries.Single(minimap.Bands, band => band.TypeId == "lava-quest").Segments.Count, "09a, 09b; 2026-10 không đặt được");

            string[] expectedMarks =
            {
                "Warning weekly-pass 2026-09-07 00:00",
                "Warning hunt-0914 2026-09-14 00:00",
                "Blocked hunt-0916-bonus 2026-09-16 12:00",
                "Warning lava-quest 2026-09-20 00:00",
                "Blocked lava-quest-2026-10 2026-10-01 00:00",
            };
            CollectionAssert.AreEqual(expectedMarks, TimelineTestQueries.Map(minimap.Marks, mark => mark.State + " " + mark.TargetId + " " +
                mark.AtUtc.ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture)).ToArray(),
                "vạch đỏ cao hết dải ở đợt bị bỏ, vạch vàng nửa trên ở cảnh báo [SD1 §3.6]");
            Assert.IsTrue(TimelineTestQueries.All(minimap.Marks, mark => mark.Finding != null), "có báo cáo thì vạch mang phát hiện để view dựng tooltip");
            Assert.LessOrEqual(LiveOpsTimelineVertexBudget.EstimateMinimap(minimap), LiveOpsTimelineVertexBudget.LaneSafetyBudget);

            LiveOpsTimelineModel noReport = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).WithCheckReport(null).Build();
            string[] droppedOnly = TimelineTestQueries.Map(noReport.Minimap.Marks, mark => mark.State + " " + mark.TargetId).ToArray();
            CollectionAssert.AreEqual(new[] { "Blocked hunt-0916-bonus", "Blocked lava-quest-2026-10" }, droppedOnly,
                "chưa kiểm vẫn có vạch đỏ cho đợt game đang bỏ theo bản biên dịch");
            Assert.AreEqual(minimap.XOf(minimap.RangeEndUtc), minimap.Width, 0.01f);
        }

        [Test]
        public void Model_HiddenLanes_RemovedFromLanesAndMinimap()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).WithHiddenLanes(new[] { "sky-race", "star-tournament" }).Build();
            CollectionAssert.AreEqual(new[] { "lava-quest", "treasure-hunt", "weekly-pass" }, TimelineTestQueries.Map(model.Lanes, lane => lane.TypeId).ToArray());
            CollectionAssert.AreEqual(new[] { "lava-quest", "treasure-hunt", "weekly-pass" }, TimelineTestQueries.Map(model.Minimap.Bands, band => band.TypeId).ToArray());
            Assert.AreEqual(new[] { 0, 1, 2 }, TimelineTestQueries.Map(model.Minimap.Bands, band => band.RowIndex).ToArray());
        }

        [Test]
        public void Model_UndeclaredType_GetsLaneSoNoEventDisappears()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-unknown", "mystery-0915", "mystery-box", "2026-09-15T00:00:00Z", "2026-09-16T00:00:00Z", "mystery"))
                .Build();
            LiveOpsTimelineModel model = new LiveOpsTimelineInput().WithDocument(document).WithNowUtc(LiveOpsDesignSample.NowUtc)
                .WithRange(Utc(9, 8), LiveOpsTimelineZoom.ThreeWeeks).WithTrackWidth(DesignTrackWidth).Build();
            CollectionAssert.AreEqual(new[] { "lava-quest", "mystery-box" }, TimelineTestQueries.Map(model.Lanes, lane => lane.TypeId).ToArray());
            LiveOpsTimelineLaneModel mystery = model.FindLane("mystery-box");
            Assert.AreEqual(LiveEventTypeColorSlots.DefaultSlotFor("mystery-box"), mystery.ColorSlot);
            Assert.AreEqual(1, mystery.Bars.Count);
            Assert.AreEqual("cố định · chưa có đợt", model.FindLane("lava-quest").MetaText);
        }

        [Test]
        public void Model_InvalidInput_ThrowsProgrammerError()
        {
            Assert.Throws<ArgumentException>(() => new LiveOpsTimelineInput().WithNowUtc(LiveOpsDesignSample.NowUtc).WithTrackWidth(DesignTrackWidth).Build());
            Assert.Throws<ArgumentOutOfRangeException>(() => TimelineDesignSampleInput.ThreeWeeks(0f).Build(), "layout chưa xong (0px) không được dựng model");
            Assert.Throws<ArgumentOutOfRangeException>(() => TimelineDesignSampleInput.ThreeWeeks(float.NaN).Build());
            Assert.Throws<ArgumentException>(() => TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).WithRange(Utc(9, 8), Utc(9, 8)).Build());
        }

        [Test]
        public void Intents_ClosedFamily_AllHandledOrIgnoredByName()
        {
            string[] expectedNames =
            {
                "SelectBarIntent", "MoveBarIntent", "AddAtTimeIntent", "DeleteBarIntent", "DuplicateBarIntent", "CopyBarIntent",
                "PasteAtTimeIntent", "OpenRuleIntent", "ShowFindingIntent", "CreateByDragIntent", "HideLaneIntent", "ShowAllLanesIntent",
                "MoveLaneIntent",
            };
            CollectionAssert.AreEqual(expectedNames, LiveOpsTimelineIntent.KnownIntentTypeNames,
                "danh sách tên cố định — presenter W4/W5 đối chiếu nhánh xử lý với danh sách này");

            Type baseType = typeof(LiveOpsTimelineIntent);
            Assert.IsTrue(baseType.IsAbstract);
            Assert.AreEqual(0, baseType.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Length,
                "không ctor public — lớp con ngoài package không lọt vào họ");
            var sortedExpectedNames = new List<string>(expectedNames);
            sortedExpectedNames.Sort(StringComparer.Ordinal);
            var actualNames = new List<string>();
            foreach (Type candidate in baseType.Assembly.GetTypes())
            {
                if (candidate == baseType || !baseType.IsAssignableFrom(candidate)) continue;
                actualNames.Add(candidate.Name);
                Assert.IsTrue(candidate.IsSealed, candidate.Name + " phải sealed để họ đóng");
            }
            actualNames.Sort(StringComparer.Ordinal);
            CollectionAssert.AreEqual(sortedExpectedNames, actualNames,
                "thêm lớp intent mà không thêm tên (hoặc ngược lại) thì presenter sẽ lặng lẽ bỏ qua ý định mới");

            var move = new MoveBarIntent("bar", Utc(9, 17), Utc(9, 18, 12), LiveOpsTimelineGesturePhase.Commit);
            Assert.IsTrue(move.IsCommit);
            Assert.IsFalse(move.IsPreview || move.IsCancel);
            var create = new CreateByDragIntent("treasure-hunt", Utc(9, 18), Utc(9, 19), LiveOpsTimelineGesturePhase.Cancel);
            Assert.IsTrue(create.IsCancel);
            Assert.AreEqual(MoveLaneIntent.Up, new MoveLaneIntent("lava-quest", -5).Direction);
            Assert.AreEqual(ShowFindingIntent.Next, new ShowFindingIntent(0).Direction);
            Assert.IsTrue(new LiveOpsTimelineHover(string.Empty, default).IsLeave);
            Assert.AreEqual(LiveOpsTimelineHitKind.None, new LiveOpsTimelineContextRequest(null, default).Hit.Kind);
        }
    }

    /// <summary>Đầu vào timeline dựng từ dữ liệu mẫu: bản so = JSON đã đăng 11/9 16:20 đọc bằng parser thật (V-3), báo cáo = validator mặc định.</summary>
    internal static class TimelineDesignSampleInput
    {
        public const string HourlyType = "hourly-bonus";

        public static LiveOpsTimelineInput ThreeWeeks(float trackWidth)
        {
            return Build(LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, LiveOpsTimelineZoom.ThreeWeeks), LiveOpsTimelineZoom.ThreeWeeks,
                trackWidth);
        }

        public static LiveOpsTimelineInput Build(DateTime rangeStartUtc, LiveOpsTimelineZoom zoom, float trackWidth)
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            DateTime nowUtc = LiveOpsDesignSample.NowUtc;
            LiveEventCalendarDocumentParseResult baseline = JsonLiveEventCalendarParser.ParseDocument(LiveOpsDesignSample.PublishedSnapshotJson);
            Assert.IsTrue(baseline.IsReadable, "JSON đã đăng mẫu phải đọc được");
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);
            LiveEventCalendarCheckReport report = LiveEventCalendarValidator.Default.Check(new LiveEventCalendarCheckContextBuilder(document, nowUtc)
                .WithCompilation(compilation)
                .WithPublishedBaseline(baseline.Document)
                .WithLatestStampBaseline(baseline.Document)
                .Build());
            LiveEventCalendarDiffResult diff = LiveEventCalendarDiff.Compare(baseline.Document, document, nowUtc);

            return new LiveOpsTimelineInput()
                .WithDocument(document)
                .WithCompilation(compilation)
                .WithCheckReport(report)
                .WithPublishedDiff(diff)
                .WithNowUtc(nowUtc)
                .WithRange(rangeStartUtc, zoom)
                .WithTrackWidth(trackWidth);
        }

        public static LiveEventCalendarDocument WithHourlyRule(LiveEventCalendarDocument document)
        {
            LiveEventCalendarDocumentBuilder builder = CopyOf(document);
            builder.WithEventType(new LiveEventTypeDefinition(HourlyType, "Thưởng mỗi giờ", 2, false, "hourly_bonus_v1"));
            builder.WithRecurringRule(new RecurringLiveEventRule(HourlyType, "2026-01-05T00:00:00Z", "hourly-", 1, 1, string.Empty));
            return builder.Build();
        }

        public static LiveEventCalendarDocument WithFixedEnd(LiveEventCalendarDocument document, string entryKey, string endUtcText)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in document.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in document.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in document.FixedEvents)
            {
                builder.WithFixedEvent(entry.EntryKey == entryKey ? entry.WithTimes(entry.StartUtcText, endUtcText) : entry);
            }
            foreach (PublishedCalendarStamp stamp in document.PublishedStamps) builder.WithPublishedStamp(stamp);
            return builder.Build();
        }

        public static LiveEventCalendarDocumentBuilder CopyOf(LiveEventCalendarDocument document)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            foreach (LiveEventTypeDefinition type in document.EventTypes) builder.WithEventType(type);
            foreach (RecurringLiveEventRule rule in document.RecurringRules) builder.WithRecurringRule(rule);
            foreach (FixedLiveEventEntry entry in document.FixedEvents) builder.WithFixedEvent(entry);
            foreach (PublishedCalendarStamp stamp in document.PublishedStamps) builder.WithPublishedStamp(stamp);
            return builder;
        }
    }

    /// <summary>
    /// Truy vấn nhỏ cho test timeline — luật package cấm System.Linq (cấp phát ẩn) kể cả trong test, nên gom vòng lặp vào đây thay vì
    /// lặp lại ở từng assert.
    /// </summary>
    internal static class TimelineTestQueries
    {
        public static List<TItem> Where<TItem>(IEnumerable<TItem> items, Predicate<TItem> predicate)
        {
            var matches = new List<TItem>();
            foreach (TItem item in items)
            {
                if (predicate(item)) matches.Add(item);
            }
            return matches;
        }

        public static TItem Single<TItem>(IEnumerable<TItem> items, Predicate<TItem> predicate)
        {
            List<TItem> matches = Where(items, predicate);
            Assert.AreEqual(1, matches.Count, "phải có đúng một phần tử khớp");
            return matches[0];
        }

        public static bool Any<TItem>(IEnumerable<TItem> items, Predicate<TItem> predicate) => Where(items, predicate).Count > 0;

        public static bool All<TItem>(IEnumerable<TItem> items, Predicate<TItem> predicate)
        {
            foreach (TItem item in items)
            {
                if (!predicate(item)) return false;
            }
            return true;
        }

        public static int Count<TItem>(IEnumerable<TItem> items, Predicate<TItem> predicate) => Where(items, predicate).Count;

        public static int Sum<TItem>(IEnumerable<TItem> items, Func<TItem, int> selector)
        {
            int total = 0;
            foreach (TItem item in items) total += selector(item);
            return total;
        }

        public static List<TResult> Map<TItem, TResult>(IEnumerable<TItem> items, Func<TItem, TResult> selector)
        {
            var results = new List<TResult>();
            foreach (TItem item in items) results.Add(selector(item));
            return results;
        }

        public static List<LiveOpsTimelineBarModel> AllBars(LiveOpsTimelineModel model)
        {
            var bars = new List<LiveOpsTimelineBarModel>();
            foreach (LiveOpsTimelineLaneModel lane in model.Lanes) bars.AddRange(lane.Bars);
            return bars;
        }
    }
}
