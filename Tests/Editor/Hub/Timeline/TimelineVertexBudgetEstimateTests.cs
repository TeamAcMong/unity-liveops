using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Ước lượng vertex (Logic, <c>-nographics</c>) — R-10: 2022.3 mất hình + log Error khi một element vượt 65.535 vertex. Hệ số là số đo
    /// SP-13 trên 2022.3 (Fill 44 vertex/hình chữ nhật, Stroke 8 vertex/đoạn). Test render thật trên 2022.3 là
    /// <c>TimelineVertexBudgetRenderTests</c> của G-TIMELINE-VIEW (W3); ở đây khoá rằng MODEL không bao giờ đưa cho view một làn vượt
    /// <see cref="LiveOpsTimelineVertexBudget.LaneSafetyBudget"/>, kể cả lịch một năm dày đặc ở mọi zoom và ở px/giờ nhỏ nhất.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class TimelineVertexBudgetEstimateTests
    {
        private const string DailyFixedType = "daily-login";
        private const string ThirteenHourType = "thirteen-hour-rush";
        private static readonly DateTime YearStartUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime YearEndUtc = new DateTime(2027, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly float[] TrackWidths = { 616f, 635f, 1500f };

        [Test]
        public void VertexBudget_Coefficients_MatchSp13()
        {
            Assert.AreEqual(65535, LiveOpsTimelineVertexBudget.MaximumVerticesPerElement);
            Assert.AreEqual(20000, LiveOpsTimelineVertexBudget.LaneSafetyBudget);
            Assert.AreEqual(44, LiveOpsTimelineVertexBudget.FillVerticesPerRectangle, "SP-13 đo 2022.3: Fill 1×1 đạt 2000/lỗi 2500, 40×8 đạt 1500/lỗi 3000");
            Assert.AreEqual(8, LiveOpsTimelineVertexBudget.StrokeVerticesPerSegment, "SP-13 đo 2022.3: Stroke đạt 8000/lỗi 12000");
            Assert.AreEqual(454, LiveOpsTimelineVertexBudget.LaneSafetyBudget / LiveOpsTimelineVertexBudget.FillVerticesPerRectangle,
                "R-10: ngân sách làn chỉ chứa ≈ 450 hình Fill");
        }

        [Test]
        public void VertexBudget_OneYearAllTypes_EveryZoom_UnderLaneSafetyBudget()
        {
            LiveEventCalendarDocument document = OneYearDocument();
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);
            var failures = new List<string>();
            int modelCount = 0;

            foreach (float trackWidth in TrackWidths)
            {
                foreach (LiveOpsTimelineZoom zoom in new[] { LiveOpsTimelineZoom.Day, LiveOpsTimelineZoom.ThreeWeeks, LiveOpsTimelineZoom.Month })
                {
                    for (DateTime rangeStartUtc = YearStartUtc; rangeStartUtc < YearEndUtc; rangeStartUtc = rangeStartUtc.AddDays(13))
                    {
                        LiveOpsTimelineModel model = NewInput(document, compilation, trackWidth).WithRange(rangeStartUtc, zoom).Build();
                        CheckModel(model, zoom + " từ " + rangeStartUtc.ToString("d/M/yyyy", CultureInfo.InvariantCulture) + " track " + trackWidth, failures);
                        modelCount++;
                    }
                }

                // Zoom liên tục nhỏ nhất: cả năm trong một khung ở 0,25 px/giờ và ở px/giờ của track (có thể lớn hơn 0,25).
                double minimumScaleHours = trackWidth / LiveOpsTimelineGeometry.MinimumPixelsPerHour;
                LiveOpsTimelineModel minimumScale = NewInput(document, compilation, trackWidth)
                    .WithRange(YearStartUtc, YearStartUtc.AddHours(minimumScaleHours)).Build();
                CheckModel(minimumScale, "0,25 px/giờ track " + trackWidth, failures);
                LiveOpsTimelineModel wholeYear = NewInput(document, compilation, trackWidth).WithRange(YearStartUtc, YearEndUtc).Build();
                CheckModel(wholeYear, "cả năm track " + trackWidth, failures);
                modelCount += 2;
            }

            Assert.IsEmpty(failures, "Làn/minimap vượt ngân sách (sẽ mất hình ở 2022.3):\n" + string.Join("\n", failures));
            Assert.Greater(modelCount, 250, "phủ đủ khung trượt qua cả năm ở ba zoom");
        }

        [Test]
        public void VertexBudget_DenseLanes_OverFourHundredFiftyBars_GroupedWithoutLosingEvents()
        {
            LiveEventCalendarDocument document = OneYearDocument();
            LiveEventCalendarCompilation compilation = LiveEventCalendarCompiler.CompileInExportOrder(document);
            const float trackWidth = 2190f;   // 8.760 giờ × 0,25 px/giờ — cả năm ở zoom nhỏ nhất
            LiveOpsTimelineModel model = NewInput(document, compilation, trackWidth).WithRange(YearStartUtc, YearEndUtc).Build();

            // 365 đợt cố định hằng ngày × ~120 vertex > 20.000: phải gom thành dải chỉ đọc, không được bỏ đợt nào.
            LiveOpsTimelineLaneModel daily = model.FindLane(DailyFixedType);
            Assert.AreEqual(365, TimelineTestQueries.Count(document.FixedEvents, entry => entry.EventType == DailyFixedType), "tiền đề: 365 đợt cố định");
            Assert.Greater(365 * (LiveOpsTimelineVertexBudget.BarFillRectangles * LiveOpsTimelineVertexBudget.FillVerticesPerRectangle +
                                  LiveOpsTimelineVertexBudget.BarStrokeSegments * LiveOpsTimelineVertexBudget.StrokeVerticesPerSegment),
                LiveOpsTimelineVertexBudget.LaneSafetyBudget, "tiền đề: vẽ từng thanh sẽ vượt ngân sách");
            Assert.IsTrue(TimelineTestQueries.Any(daily.Bars, bar => bar.Source == LiveOpsTimelineBarSource.FixedStrip), "làn cố định dày đặc gom thành dải");
            Assert.AreEqual(365, TimelineTestQueries.Sum(daily.Bars, bar => bar.StripCount), "gom dải không làm mất đợt nào");
            Assert.LessOrEqual(LiveOpsTimelineVertexBudget.EstimateLane(daily, model.Geometry), LiveOpsTimelineVertexBudget.LaneSafetyBudget);

            // Luật 13 giờ/chạy 1 giờ: khe nghỉ 12 giờ × 0,25 ≈ 3px, sát ngưỡng gom — dù bậc nào gom thì 674 lần lặp vẫn phải đủ và làn vừa ngân sách.
            LiveOpsTimelineLaneModel thirteenHour = model.FindLane(ThirteenHourType);
            int expectedOccurrences = CountOccurrences(compilation, ThirteenHourType, YearStartUtc, YearEndUtc);
            Assert.Greater(expectedOccurrences, 450, "tiền đề: quá ≈ 450 thanh trong làn");
            Assert.AreEqual(expectedOccurrences, TimelineTestQueries.Sum(thirteenHour.Bars, bar => bar.StripCount));
            Assert.IsTrue(TimelineTestQueries.Any(thirteenHour.Bars, bar => bar.Source == LiveOpsTimelineBarSource.RecurringStrip));
            Assert.LessOrEqual(LiveOpsTimelineVertexBudget.EstimateLane(thirteenHour, model.Geometry), LiveOpsTimelineVertexBudget.LaneSafetyBudget);

            // Zoom vào lại (3 tuần) thì đợt cố định tách lại thành thanh kéo được.
            LiveOpsTimelineModel zoomedIn = NewInput(document, compilation, 635f).WithRange(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
                LiveOpsTimelineZoom.ThreeWeeks).Build();
            Assert.IsTrue(TimelineTestQueries.All(zoomedIn.FindLane(DailyFixedType).Bars, bar => bar.Source == LiveOpsTimelineBarSource.Fixed));
            Assert.AreEqual(21, zoomedIn.FindLane(DailyFixedType).Bars.Count);
        }

        [Test]
        public void VertexBudget_EstimateLane_CountsBarsOverlapAndBackground()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(635f).Build();
            LiveOpsTimelineLaneModel treasureHunt = model.FindLane("treasure-hunt");

            const int fill = LiveOpsTimelineVertexBudget.FillVerticesPerRectangle;
            const int stroke = LiveOpsTimelineVertexBudget.StrokeVerticesPerSegment;
            int background = 22 * stroke + 4 * fill + 2 * fill;               // 21 ngày → 22 vạch, 3 tuần → 4 cột cuối tuần, hôm nay + bây giờ
            int overlapWidthHatch = (int)Math.Ceiling((15.12f + 2 * LiveOpsTimelineGeometry.RowPitch) / LiveOpsTimelineVertexBudget.HatchPeriod);
            int overlap = 2 * fill + 2 * stroke + overlapWidthHatch * stroke;
            int bars = 2 * (2 * fill + 4 * stroke) + 1 * stroke + 2 * fill + 2 * fill;   // 2 thanh + gạch bị bỏ + 2 vuông khác bản đăng + tay nắm hover
            Assert.AreEqual(background + overlap + bars, LiveOpsTimelineVertexBudget.EstimateLane(treasureHunt, model.Geometry));

            int minimapExpected = TimelineTestQueries.Sum(model.Minimap.Bands, band => band.Segments.Count) * fill + model.Minimap.Marks.Count * fill + fill +
                                  (fill + 2 * stroke) + 4 * stroke;
            Assert.AreEqual(minimapExpected, LiveOpsTimelineVertexBudget.EstimateMinimap(model.Minimap));
        }

        private static void CheckModel(LiveOpsTimelineModel model, string label, List<string> failures)
        {
            foreach (LiveOpsTimelineLaneModel lane in model.Lanes)
            {
                int estimate = LiveOpsTimelineVertexBudget.EstimateLane(lane, model.Geometry);
                if (estimate > LiveOpsTimelineVertexBudget.LaneSafetyBudget) failures.Add(label + " · làn " + lane.TypeId + " = " + estimate + " vertex, " + lane.Bars.Count + " thanh");
            }
            int minimapEstimate = LiveOpsTimelineVertexBudget.EstimateMinimap(model.Minimap);
            if (minimapEstimate > LiveOpsTimelineVertexBudget.LaneSafetyBudget) failures.Add(label + " · minimap = " + minimapEstimate + " vertex");
        }

        private static LiveOpsTimelineInput NewInput(LiveEventCalendarDocument document, LiveEventCalendarCompilation compilation, float trackWidth)
        {
            return new LiveOpsTimelineInput()
                .WithDocument(document)
                .WithCompilation(compilation)
                .WithNowUtc(LiveOpsDesignSample.NowUtc)
                .WithTrackWidth(trackWidth);
        }

        private static int CountOccurrences(LiveEventCalendarCompilation compilation, string eventType, DateTime fromUtc, DateTime toUtc)
        {
            RecurringLiveEventCalendar calendar = TimelineTestQueries.Single(compilation.RecurringCalendars, candidate => candidate.EventType == eventType);
            int count = 0;
            for (long index = calendar.OccurrenceIndexAt(fromUtc); ; index++)
            {
                LiveEventInstance occurrence = calendar.GetOccurrence(index);
                if (occurrence.StartUtc >= toUtc) break;
                if (occurrence.EndUtc > fromUtc) count++;
            }
            return count;
        }

        /// <summary>
        /// Lịch một năm đủ mọi kiểu làn: năm loại mẫu (luật 7 ngày, luật 24/20 giờ, đợt 3 ngày mỗi tháng, săn kho báu mỗi tuần kèm đợt thưởng
        /// chồng giờ, giải đấu mỗi tháng) + luật hằng giờ liền nhau + luật 13/1 giờ + đợt cố định hằng ngày.
        /// </summary>
        private static LiveEventCalendarDocument OneYearDocument()
        {
            LiveEventCalendarDocumentBuilder builder = TimelineDesignSampleInput.CopyOf(TimelineDesignSampleInput.WithHourlyRule(LiveOpsDesignSample.Document));
            builder.WithEventType(new LiveEventTypeDefinition(ThirteenHourType, "Đua 13 giờ", 3, false, "rush_v1"));
            builder.WithRecurringRule(new RecurringLiveEventRule(ThirteenHourType, "2026-01-05T00:00:00Z", "rush-", 13, 1, string.Empty));
            builder.WithEventType(new LiveEventTypeDefinition(DailyFixedType, "Đăng nhập mỗi ngày", 5, false, "daily_login_v1"));

            int sequence = 0;
            for (DateTime day = YearStartUtc; day < YearEndUtc; day = day.AddDays(1))
            {
                AddFixed(builder, ref sequence, DailyFixedType, "login-" + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture), day, day.AddHours(23));
            }
            for (DateTime month = YearStartUtc; month < YearEndUtc; month = month.AddMonths(1))
            {
                string suffix = month.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                AddFixed(builder, ref sequence, "lava-quest", "lava-quest-y" + suffix, month.AddDays(9), month.AddDays(12));
                AddFixed(builder, ref sequence, "star-tournament", "star-tournament-y" + suffix, month.AddDays(2), month.AddDays(5));
            }
            for (DateTime week = YearStartUtc; week < YearEndUtc; week = week.AddDays(7))
            {
                string suffix = week.ToString("MMdd-yyyy", CultureInfo.InvariantCulture);
                AddFixed(builder, ref sequence, "treasure-hunt", "hunt-y" + suffix, week, week.AddDays(3));
                AddFixed(builder, ref sequence, "treasure-hunt", "hunt-bonus-y" + suffix, week.AddDays(2).AddHours(12), week.AddDays(4));
            }
            return builder.Build();
        }

        private static void AddFixed(LiveEventCalendarDocumentBuilder builder, ref int sequence, string eventType, string eventId, DateTime startUtc, DateTime endUtc)
        {
            builder.WithFixedEvent(new FixedLiveEventEntry("year-" + sequence.ToString(CultureInfo.InvariantCulture), eventId, eventType,
                LiveEventUtcText.Format(startUtc), LiveEventUtcText.Format(endUtc), string.Empty));
            sequence++;
        }
    }
}
