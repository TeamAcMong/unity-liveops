using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Hình học timeline (Logic, <c>-nographics</c>): toạ độ Hình 1 [SD1 §3.3] dung sai 0,5px, bảng zoom [SD1 §3.2], thuật toán nhãn thanh
    /// [SD1 §3.5], bắt lưới, zoom liên tục tại con trỏ và thước (V-11). Toạ độ lấy từ model dựng bằng dữ liệu mẫu — kiểm cùng lúc công
    /// thức hình học và việc model đặt đúng thanh nào vào khung.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class TimelineGeometryTests
    {
        private const float PixelTolerance = 0.5f;
        private const float DesignTrackWidth = 635f;
        private const float NarrowTrackWidth = 616f;

        private static DateTime Utc(int month, int day, int hour = 0, int minute = 0)
        {
            return new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        [Test]
        public void Geometry_Figure1_BarPositions()
        {
            LiveOpsTimelineModel model = TimelineDesignSampleInput.ThreeWeeks(DesignTrackWidth).Build();
            LiveOpsTimelineGeometry geometry = model.Geometry;

            Assert.AreEqual(Utc(9, 8), model.RangeStartUtc, "Hình 1: khoảng 3 tuần 8/9 – 29/9.");
            Assert.AreEqual(Utc(9, 29), model.RangeEndUtc);
            Assert.AreEqual(30.24, geometry.PixelsPerHour * 24, 0.005, "30,24 px/ngày ở track 635.");

            LiveOpsTimelineLaneModel weeklyPass = model.FindLane("weekly-pass");
            AssertBar(geometry, Single(weeklyPass, "pass-35"), 0f, 181.4f, true, false, "weekly-pass-35 bắt đầu 7/9 trước khung: dấu ◂");
            AssertBar(geometry, Single(weeklyPass, "pass-36"), 181.4f, 393.1f, false, false, "pass-36");
            AssertBar(geometry, Single(weeklyPass, "pass-37"), 393.1f, 604.8f, false, false, "pass-37");
            AssertBar(geometry, Single(weeklyPass, "pass-38"), 604.8f, 635f, false, true, "pass-38 kết thúc 5/10 sau khung: dấu ▸");
            Assert.AreEqual(4, weeklyPass.Bars.Count);

            LiveOpsTimelineLaneModel skyRace = model.FindLane("sky-race");
            Assert.AreEqual(21, skyRace.Bars.Count, "sky-race-246 … sky-race-266: khe nghỉ 5px ≥ 3px nên không gom.");
            for (int dayIndex = 0; dayIndex < 21; dayIndex++)
            {
                LiveOpsTimelineBarModel bar = skyRace.Bars[dayIndex];
                Assert.AreEqual("sky-race-" + (246 + dayIndex), bar.EventId);
                Assert.AreEqual(LiveOpsTimelineBarSource.Recurring, bar.Source);
                float expectedLeft = 30.24f * dayIndex;
                AssertBar(geometry, bar, expectedLeft, expectedLeft + 25.2f, false, false, bar.EventId + " (30,24 × k, rộng 25,2)");
            }

            LiveOpsTimelineLaneModel lavaQuest = model.FindLane("lava-quest");
            AssertBar(geometry, Single(lavaQuest, "lava-quest-2026-09a"), 60.5f, 151.2f, false, false, "09a");
            AssertBar(geometry, Single(lavaQuest, "lava-quest-2026-09b"), 272.1f, 362.9f, false, false, "09b");

            LiveOpsTimelineLaneModel treasureHunt = model.FindLane("treasure-hunt");
            AssertBar(geometry, Single(treasureHunt, "hunt-0914"), 181.4f, 272.1f, false, false, "hunt-0914");
            AssertBar(geometry, Single(treasureHunt, "hunt-0916-bonus"), 257.0f, 302.4f, false, false, "hunt-0916-bonus");
            Assert.AreEqual(1, treasureHunt.OverlapRanges.Count);
            Assert.AreEqual(257.0f, geometry.XOf(treasureHunt.OverlapRanges[0].startUtc), PixelTolerance, "vùng chồng bắt đầu 257,0");
            Assert.AreEqual(272.1f, geometry.XOf(treasureHunt.OverlapRanges[0].endUtc), PixelTolerance, "vùng chồng kết thúc 272,1 (15px)");

            Assert.AreEqual(162.3f, geometry.XOf(LiveOpsDesignSample.NowUtc), PixelTolerance, "vạch bây giờ cách mép trái track 162px");
            Assert.AreEqual(257.0f, geometry.XOf(Utc(9, 16, 12)), PixelTolerance, "con trỏ ở 16/9 12:00");
            Assert.AreEqual(Utc(9, 16, 12), LiveOpsTimelineGeometry.Snap(geometry.TimeAt(257.0f), TimeSpan.FromHours(1)),
                "TimeAt là nghịch đảo của XOf — bubble con trỏ đọc lại đúng giờ đã bắt lưới");

            Assert.AreEqual(0, model.FindLane("star-tournament").Bars.Count, "star-tournament-2026-10 ngoài khung — chỉ có chip Đợt tới");
            Assert.AreEqual(0, TimelineTestQueries.Count(lavaQuest.Bars, bar => bar.EventId == "lava-quest-2026-10"), "lava-quest-2026-10 không đặt được lên trục");
        }

        [Test]
        public void Geometry_ZoomTable()
        {
            DateTime nowUtc = LiveOpsDesignSample.NowUtc;

            Assert.AreEqual(TimeSpan.FromHours(24), LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.Day), "PD-18: zoom Ngày = khoảng cố định 24 giờ");
            Assert.AreEqual(TimeSpan.FromDays(21), LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.ThreeWeeks));
            Assert.AreEqual(TimeSpan.FromDays(42), LiveOpsTimelineGeometry.RangeLengthOf(LiveOpsTimelineZoom.Month));

            Assert.AreEqual(Utc(9, 13), LiveOpsTimelineGeometry.RangeStartFor(nowUtc, LiveOpsTimelineZoom.Day));
            Assert.AreEqual(Utc(9, 8), LiveOpsTimelineGeometry.RangeStartFor(nowUtc, LiveOpsTimelineZoom.ThreeWeeks), "Hình 1: 8/9 – 29/9");
            Assert.AreEqual(Utc(9, 8), LiveOpsTimelineGeometry.RangeStartFor(nowUtc, LiveOpsTimelineZoom.Month), "Hình 12 khung 11: 42 ngày từ 8/9");

            AssertZoomRow(LiveOpsTimelineZoom.Day, DesignTrackWidth, 635.0 / 24, TimeSpan.FromMinutes(15));
            AssertZoomRow(LiveOpsTimelineZoom.ThreeWeeks, DesignTrackWidth, 1.26, TimeSpan.FromHours(1));
            AssertZoomRow(LiveOpsTimelineZoom.Month, DesignTrackWidth, 0.63, TimeSpan.FromDays(1));
            AssertZoomRow(LiveOpsTimelineZoom.ThreeWeeks, NarrowTrackWidth, 1.22, TimeSpan.FromHours(1));

            Assert.AreEqual(0.25, LiveOpsTimelineGeometry.MinimumPixelsPerHour);
            Assert.AreEqual(48, LiveOpsTimelineGeometry.MaximumPixelsPerHour);
            Assert.AreEqual(168f, LiveOpsTimelineGeometry.LaneHeaderWidth);
            Assert.AreEqual(803f, LiveOpsTimelineGeometry.LaneHeaderWidth + DesignTrackWidth, "Timeline 803px = header 168 + track 635");
        }

        [Test]
        public void Geometry_AutoSnap()
        {
            Assert.AreEqual(TimeSpan.FromMinutes(15), LiveOpsTimelineGeometry.AutoSnapStep(48));
            Assert.AreEqual(TimeSpan.FromMinutes(15), LiveOpsTimelineGeometry.AutoSnapStep(6));
            Assert.AreEqual(TimeSpan.FromHours(1), LiveOpsTimelineGeometry.AutoSnapStep(5.99));
            Assert.AreEqual(TimeSpan.FromHours(1), LiveOpsTimelineGeometry.AutoSnapStep(0.8));
            Assert.AreEqual(TimeSpan.FromDays(1), LiveOpsTimelineGeometry.AutoSnapStep(0.79));
            Assert.AreEqual(TimeSpan.FromDays(1), LiveOpsTimelineGeometry.AutoSnapStep(0.25));

            Assert.AreEqual(Utc(9, 16, 12), LiveOpsTimelineGeometry.Snap(Utc(9, 16, 12, 7), TimeSpan.FromMinutes(15)));
            Assert.AreEqual(Utc(9, 16, 12, 15), LiveOpsTimelineGeometry.Snap(Utc(9, 16, 12, 8), TimeSpan.FromMinutes(15)));
            Assert.AreEqual(Utc(9, 16, 12), LiveOpsTimelineGeometry.Snap(Utc(9, 16, 11, 40), TimeSpan.FromHours(1)));
            Assert.AreEqual(Utc(9, 17), LiveOpsTimelineGeometry.Snap(Utc(9, 16, 13), TimeSpan.FromDays(1)));
            Assert.AreEqual(Utc(9, 16), LiveOpsTimelineGeometry.Snap(Utc(9, 16, 11), TimeSpan.FromDays(1)));
        }

        [Test]
        public void Geometry_BarLabel_Cases()
        {
            // [SD1 §3.5] room = w − 12 − (changed ? 8 : 0); < 24px hoặc room < 8 → không nhãn.
            Assert.AreEqual(string.Empty, LiveOpsTimelineGeometry.BarLabel("pass-36", 23.9f, true, false), "< 24px: không nhãn");
            Assert.AreEqual(string.Empty, LiveOpsTimelineGeometry.BarLabel("lava-quest-2026-09b", 27f, false, true), "room 7 < 8: không nhãn");

            // 24–64px: số thứ tự khi hậu tố khớp ^\d{1,4}[a-z]?$ và vừa room.
            Assert.AreEqual("38", LiveOpsTimelineGeometry.BarLabel("pass-38", 29.2f, true, false), "Hình 1: pass-38 cắt mép phải, nhãn \"38\"");
            Assert.AreEqual("09b", LiveOpsTimelineGeometry.BarLabel("lava-quest-2026-09b", 40f, false, true));
            Assert.AreEqual(string.Empty, LiveOpsTimelineGeometry.BarLabel("hunt-0916-bonus", 44.4f, false, false),
                "hậu tố \"bonus\" không phải số thứ tự: để trống, dựa vào tooltip");
            Assert.AreEqual(string.Empty, LiveOpsTimelineGeometry.BarLabel("sky-race-246", 24.2f, true, false), "\"246\" rộng 16,8 > room 12,2");
            Assert.AreEqual(string.Empty, LiveOpsTimelineGeometry.BarLabel("pass-12345", 60f, true, false), "hậu tố quá 4 chữ số");

            // ≥ 64px: id đầy đủ; dài quá thì cắt giữa ceil(k/2) đầu + "…" + floor(k/2) cuối, k = max(3, max − 1).
            Assert.AreEqual("hunt-0914", LiveOpsTimelineGeometry.BarLabel("hunt-0914", 89.7f, false, false));
            Assert.AreEqual("weekly-pass-35", LiveOpsTimelineGeometry.BarLabel("weekly-pass-35", 180.4f, true, true));
            Assert.AreEqual("lava-q…6-09b", LiveOpsTimelineGeometry.BarLabel("lava-quest-2026-09b", 89.8f, false, true),
                "room 69,8 → tối đa 12 ký tự → k = 11");
            Assert.AreEqual("star-t…26-10", LiveOpsTimelineGeometry.BarLabel("star-tournament-2026-10", 80f, false, false));
            Assert.AreEqual("sky-…246", LiveOpsTimelineGeometry.BarLabel("sky-race-246", 70f, true, false), "icon lặp trừ 13px trước khi chia");
            Assert.AreEqual(string.Empty, LiveOpsTimelineGeometry.BarLabel(null, 200f, false, false));
        }

        [Test]
        public void Geometry_ZoomAround_KeepsAnchorTime()
        {
            DateTime rangeStartUtc = Utc(9, 8);
            const double pixelsPerHour = DesignTrackWidth / (21.0 * 24);
            const float anchorX = 257f;
            DateTime anchorUtc = new LiveOpsTimelineGeometry(rangeStartUtc, rangeStartUtc.AddDays(21), DesignTrackWidth).TimeAt(anchorX);

            foreach (double wheelDelta in new[] { -1.0, 1.0, 3.0, -2.5 })
            {
                (DateTime nextStartUtc, double nextScale) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, pixelsPerHour, wheelDelta, anchorX);
                Assert.AreEqual(pixelsPerHour * Math.Pow(1.15, -wheelDelta), nextScale, 1e-9, "mỗi nấc × hoặc ÷ 1,15 — nấc " + wheelDelta);
                var nextGeometry = new LiveOpsTimelineGeometry(nextStartUtc, nextStartUtc.AddHours(DesignTrackWidth / nextScale), DesignTrackWidth);
                Assert.That(Math.Abs((nextGeometry.TimeAt(anchorX) - anchorUtc).TotalSeconds), Is.LessThan(1.0),
                    "thời điểm dưới con trỏ phải đứng yên khi zoom (nấc " + wheelDelta + ")");
            }

            (DateTime zoomedIn, double zoomedInScale) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, pixelsPerHour, -1, anchorX);
            Assert.Greater(zoomedInScale, pixelsPerHour, "cuộn lên (delta âm) = phóng to");
            Assert.Greater(zoomedIn, rangeStartUtc, "phóng to quanh con trỏ giữa track thì đầu khung dời sang phải");

            // Trackpad: hai nửa nấc cộng dồn = một nấc.
            (DateTime halfStart, double halfScale) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, pixelsPerHour, 0.5, anchorX);
            (DateTime twoHalvesStart, double twoHalvesScale) = LiveOpsTimelineGeometry.ZoomAround(halfStart, halfScale, 0.5, anchorX);
            (DateTime oneStepStart, double oneStepScale) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, pixelsPerHour, 1.0, anchorX);
            Assert.AreEqual(oneStepScale, twoHalvesScale, 1e-9);
            Assert.That(Math.Abs((twoHalvesStart - oneStepStart).TotalSeconds), Is.LessThan(1.0));

            DateTime scrolled = LiveOpsTimelineGeometry.ScrollBy(rangeStartUtc, pixelsPerHour, 2);
            Assert.AreEqual(2 * LiveOpsTimelineGeometry.ScrollPixelsPerWheelNotch / pixelsPerHour, (scrolled - rangeStartUtc).TotalHours, 1e-3,
                "Shift + bánh xe dời một số pixel cố định mỗi nấc, quy ra giờ theo zoom");
        }

        [Test]
        public void Geometry_ZoomAround_ClampsScale()
        {
            DateTime rangeStartUtc = Utc(9, 8);
            const float anchorX = 300f;

            (DateTime maximumStart, double maximumScale) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, 1.26, -100, anchorX);
            Assert.AreEqual(LiveOpsTimelineGeometry.MaximumPixelsPerHour, maximumScale, "kẹp trên 48 px/giờ");
            (DateTime minimumStart, double minimumScale) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, 1.26, 100, anchorX);
            Assert.AreEqual(LiveOpsTimelineGeometry.MinimumPixelsPerHour, minimumScale, "kẹp dưới 0,25 px/giờ");

            DateTime anchorUtc = rangeStartUtc.AddHours(anchorX / 1.26);
            Assert.That(Math.Abs((maximumStart.AddHours(anchorX / maximumScale) - anchorUtc).TotalSeconds), Is.LessThan(1.0),
                "chạm trần vẫn giữ thời điểm dưới con trỏ");
            Assert.That(Math.Abs((minimumStart.AddHours(anchorX / minimumScale) - anchorUtc).TotalSeconds), Is.LessThan(1.0));

            (DateTime _, double alreadyAbove) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, 60, 0, anchorX);
            Assert.AreEqual(LiveOpsTimelineGeometry.MaximumPixelsPerHour, alreadyAbove, "px/giờ đầu vào ngoài khoảng cũng bị kẹp");
            (DateTime _, double alreadyBelow) = LiveOpsTimelineGeometry.ZoomAround(rangeStartUtc, 0.01, 0, anchorX);
            Assert.AreEqual(LiveOpsTimelineGeometry.MinimumPixelsPerHour, alreadyBelow);

            (DateTime nearMinimum, double _) = LiveOpsTimelineGeometry.ZoomAround(DateTime.MinValue, 1.26, 100, anchorX);
            Assert.AreEqual(DateTime.MinValue.Ticks, nearMinimum.Ticks, "sát mép DateTime thì kẹp, không ném");
        }

        [Test]
        public void Geometry_RulerTicks_DayZoomHasDeviceTier()
        {
            TimeSpan deviceOffset = LiveOpsDesignSample.DeviceOffset;

            var dayGeometry = new LiveOpsTimelineGeometry(Utc(9, 13), Utc(9, 14), DesignTrackWidth);
            IReadOnlyList<LiveOpsTimelineRulerTick> dayTicks = LiveOpsTimelineGeometry.RulerTicks(dayGeometry, LiveOpsTimelineZoom.Day, deviceOffset);
            List<LiveOpsTimelineRulerTick> deviceTier = TimelineTestQueries.Where(dayTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DeviceTime);
            List<LiveOpsTimelineRulerTick> hourTier = TimelineTestQueries.Where(dayTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DayOrHour);
            Assert.AreEqual(8, deviceTier.Count, "tầng 3 giờ máy mỗi 3 giờ trong 24 giờ");
            Assert.AreEqual("07:00", deviceTier[0].Text, "UTC 00:00 = 07:00 giờ máy UTC+7 (V-11)");
            Assert.AreEqual(0f, deviceTier[0].X, PixelTolerance);
            Assert.AreEqual("10:00", deviceTier[1].Text);
            CollectionAssert.AreEqual(new[] { "00", "03", "06", "09", "12", "15", "18", "21" }, TimelineTestQueries.Map(hourTier, tick => tick.Text).ToArray(),
                "tầng 2 zoom Ngày = giờ UTC");
            for (int index = 0; index < deviceTier.Count; index++)
            {
                Assert.AreEqual(hourTier[index].X, deviceTier[index].X, "giờ máy nằm đúng dưới giờ UTC cùng thời điểm");
            }
            Assert.IsTrue(TimelineTestQueries.Any(dayTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.MonthAndWeek && tick.Text == "THÁNG 9 2026"));

            var threeWeeksGeometry = new LiveOpsTimelineGeometry(Utc(9, 8), Utc(9, 29), DesignTrackWidth);
            IReadOnlyList<LiveOpsTimelineRulerTick> threeWeeksTicks =
                LiveOpsTimelineGeometry.RulerTicks(threeWeeksGeometry, LiveOpsTimelineZoom.ThreeWeeks, deviceOffset);
            Assert.IsFalse(TimelineTestQueries.Any(threeWeeksTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DeviceTime), "3 tuần chỉ có hai tầng");
            LiveOpsTimelineRulerTick monday = TimelineTestQueries.Single(threeWeeksTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DayOrHour && tick.Text == "T2 14");
            Assert.IsTrue(monday.IsEmphasized, "thứ Hai \"T2 14\" đậm");
            Assert.AreEqual(181.4f, monday.X, PixelTolerance);
            LiveOpsTimelineRulerTick sunday = TimelineTestQueries.Single(threeWeeksTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DayOrHour && tick.Text == "13");
            Assert.IsFalse(sunday.IsEmphasized);
            Assert.AreEqual(21, TimelineTestQueries.Count(threeWeeksTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DayOrHour));
            List<string> weekLabels = TimelineTestQueries.Map(TimelineTestQueries.Where(threeWeeksTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.MonthAndWeek && tick.Text.StartsWith("Tuần", StringComparison.Ordinal)),
                tick => tick.Text);
            CollectionAssert.AreEqual(new[] { "Tuần 38", "Tuần 39" }, weekLabels, "Tuần 40 ở 28/9 còn 30px < 48px tới mép phải: bỏ");
            LiveOpsTimelineRulerTick lastMondayLine = TimelineTestQueries.Single(threeWeeksTicks,
                tick => tick.Tier == LiveOpsTimelineRulerTier.MonthAndWeek && tick.TimeUtc == Utc(9, 28));
            Assert.AreEqual(string.Empty, lastMondayLine.Text, "chỉ bỏ nhãn \"Tuần 40\"");
            Assert.IsTrue(lastMondayLine.HasLine, "vạch thứ Hai 28/9 ở tầng 1 vẫn giữ [SD1 §3.2]");

            var monthGeometry = new LiveOpsTimelineGeometry(Utc(9, 8), Utc(10, 20), DesignTrackWidth);
            IReadOnlyList<LiveOpsTimelineRulerTick> monthTicks = LiveOpsTimelineGeometry.RulerTicks(monthGeometry, LiveOpsTimelineZoom.Month, deviceOffset);
            List<LiveOpsTimelineRulerTick> monthSecondTier = TimelineTestQueries.Where(monthTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DayOrHour);
            Assert.AreEqual("8/9", monthSecondTier[0].Text, "tuần đầu cụt chỉ ghi ngày");
            Assert.IsFalse(monthSecondTier[0].HasLine);
            Assert.IsFalse(monthSecondTier[0].IsEmphasized);
            Assert.AreEqual("T2 14/9", monthSecondTier[1].Text);
            Assert.IsTrue(monthSecondTier[1].IsEmphasized);
            Assert.IsFalse(TimelineTestQueries.Any(monthSecondTier, tick => tick.Text == "T2 19/10"), "nhãn tuần còn < 44px tới mép phải thì bỏ");
            LiveOpsTimelineRulerTick lastMonthMonday = TimelineTestQueries.Single(monthSecondTier, tick => tick.TimeUtc == Utc(10, 19));
            Assert.AreEqual(string.Empty, lastMonthMonday.Text);
            Assert.IsTrue(lastMonthMonday.HasLine, "Hình 12 khung 11: bỏ nhãn nhưng vạch thứ Hai 19/10 vẫn có");
            Assert.IsFalse(TimelineTestQueries.Any(monthTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.DeviceTime));
            Assert.IsTrue(TimelineTestQueries.Any(monthTicks, tick => tick.Tier == LiveOpsTimelineRulerTier.MonthAndWeek && tick.Text == "THÁNG 10 2026"));
        }

        private static void AssertZoomRow(LiveOpsTimelineZoom zoom, float trackWidth, double expectedPixelsPerHour, TimeSpan expectedSnap)
        {
            DateTime rangeStartUtc = LiveOpsTimelineGeometry.RangeStartFor(LiveOpsDesignSample.NowUtc, zoom);
            var geometry = new LiveOpsTimelineGeometry(rangeStartUtc, rangeStartUtc + LiveOpsTimelineGeometry.RangeLengthOf(zoom), trackWidth);
            Assert.AreEqual(expectedPixelsPerHour, geometry.PixelsPerHour, 0.005, zoom + " ở track " + trackWidth + ": px/giờ = bề rộng track ÷ số giờ");
            Assert.AreEqual(expectedSnap, LiveOpsTimelineGeometry.AutoSnapStep(geometry.PixelsPerHour), zoom + ": bước bắt lưới tự động");
            Assert.AreEqual(trackWidth, geometry.XOf(geometry.RangeEndUtc), 0.01f, "khoảng preset vừa đúng bề rộng track");
        }

        /// <summary>
        /// (nợ W4 D-3(c)) Bước bắt lưới của cử chỉ kéo phải là bước THẬT của khung nhìn, do menu "Bắt lưới" bơm xuống — không còn
        /// luôn luôn tự tính theo zoom. Chọn "1 ngày" xong kéo mà vẫn nhích 15 phút là menu nói một đằng, tay làm một nẻo.
        /// </summary>
        [Test]
        public void DragController_UsesViewportSnapStep()
        {
            LiveOpsTimelineModel model = TimelineViewInputs.Checked(LiveOpsDesignSample.Document)()
                .WithRange(Utc(9, 8), Utc(9, 29))
                .WithTrackWidth(DesignTrackWidth)
                .Build();
            LiveOpsTimelineLaneModel lane = LaneOf(model, "treasure-hunt");
            LiveOpsTimelineBarModel bar = Single(lane, "hunt-0916-bonus");
            LiveOpsTimelineGeometry geometry = model.Geometry;
            float startX = geometry.XOf(bar.StartUtc);
            // Kéo 10 giờ: bước tự động ở zoom 3 tuần là 1 giờ, nên mặc định phải rơi đúng +10 giờ.
            float tenHours = (float)(geometry.PixelsPerHour * 10d);

            LiveOpsTimelineDragController automatic = new LiveOpsTimelineDragController();
            Assert.IsTrue(automatic.BeginBar(lane, geometry, bar, LiveOpsTimelineBarRegion.Body, startX));
            automatic.Move(startX + tenHours, false);
            Assert.AreEqual(bar.StartUtc.AddHours(10), automatic.PreviewStartUtc, "Tự động = bước theo zoom (1 giờ ở 3 tuần)");

            LiveOpsTimelineDragController daily = new LiveOpsTimelineDragController { SnapStep = TimeSpan.FromDays(1) };
            Assert.IsTrue(daily.BeginBar(lane, geometry, bar, LiveOpsTimelineBarRegion.Body, startX));
            daily.Move(startX + tenHours, false);
            Assert.AreEqual(0L, daily.PreviewStartUtc.TimeOfDay.Ticks, "bắt lưới 1 ngày: giờ xem trước rơi đúng 00:00 UTC");

            LiveOpsTimelineDragController off = new LiveOpsTimelineDragController { SnapStep = TimeSpan.Zero };
            Assert.IsTrue(off.BeginBar(lane, geometry, bar, LiveOpsTimelineBarRegion.Body, startX));
            off.Move(startX + tenHours + 3f, false);
            Assert.AreNotEqual(0, off.PreviewStartUtc.Ticks % TimeSpan.TicksPerHour,
                "Tắt bắt lưới: giờ xem trước không bị kéo về mốc tròn nào");
        }

        /// <summary>
        /// (nợ W4 D-3(c), phần còn thiếu) Bước bắt lưới phải đi tới CẢ giờ-tại-con-trỏ và bước nhích bàn phím, không chỉ cử chỉ
        /// kéo: giờ con trỏ là thứ đi vào nhãn menu "Thêm đợt bắt đầu … UTC…" và vào lệnh Dán, còn ← → là đường sửa bằng bàn phím.
        /// Trước bản vá, chọn "Bắt lưới: 1 ngày" xong nhấn → vẫn nhích đúng 15 phút.
        /// </summary>
        [Test]
        public void EffectiveStepFor_IsSharedByCursorAndKeyboardPaths()
        {
            LiveOpsTimelineDragController automatic = new LiveOpsTimelineDragController();
            LiveOpsTimelineDragController daily = new LiveOpsTimelineDragController { SnapStep = TimeSpan.FromDays(1) };
            LiveOpsTimelineDragController off = new LiveOpsTimelineDragController { SnapStep = TimeSpan.Zero };
            const double dayZoomPixelsPerHour = 60d;

            Assert.AreEqual(LiveOpsTimelineGeometry.AutoSnapStep(dayZoomPixelsPerHour), automatic.EffectiveStepFor(dayZoomPixelsPerHour),
                "Tự động = bước theo zoom, đúng thứ hai đường kia vẫn dùng khi người dùng chưa chọn gì");
            Assert.AreEqual(TimeSpan.FromDays(1), daily.EffectiveStepFor(dayZoomPixelsPerHour), "chọn 1 ngày thì mọi đường bắt 1 ngày");
            Assert.AreEqual(TimeSpan.Zero, off.EffectiveStepFor(dayZoomPixelsPerHour), "Tắt = không bắt lưới; nơi gọi tự quyết cách xử");
        }

        private static LiveOpsTimelineLaneModel LaneOf(LiveOpsTimelineModel model, string typeId)
        {
            for (int index = 0; index < model.Lanes.Count; index++)
            {
                if (model.Lanes[index].TypeId == typeId) return model.Lanes[index];
            }
            Assert.Fail("model thiếu làn " + typeId);
            return null;
        }

        private static LiveOpsTimelineBarModel Single(LiveOpsTimelineLaneModel lane, string eventId)
        {
            List<LiveOpsTimelineBarModel> matches = TimelineTestQueries.Where(lane.Bars, bar => bar.EventId == eventId);
            Assert.AreEqual(1, matches.Count, "làn " + lane.TypeId + " phải có đúng một thanh " + eventId + ": " + string.Join(" | ", lane.Bars));
            return matches[0];
        }

        private static void AssertBar(LiveOpsTimelineGeometry geometry, LiveOpsTimelineBarModel bar, float expectedLeft, float expectedRight,
            bool expectedClippedStart, bool expectedClippedEnd, string label)
        {
            (float left, float width, bool clippedStart, bool clippedEnd) = geometry.BarRect(bar.StartUtc, bar.EndUtc);
            Assert.AreEqual(expectedLeft, left, PixelTolerance, label + ": x1");
            Assert.AreEqual(expectedRight, left + width + 1f, PixelTolerance, label + ": x2 (rộng = x2 − x1 − 1)");
            Assert.AreEqual(expectedClippedStart, clippedStart, label + ": cắt mép trái");
            Assert.AreEqual(expectedClippedEnd, clippedEnd, label + ": cắt mép phải");
        }
    }
}
