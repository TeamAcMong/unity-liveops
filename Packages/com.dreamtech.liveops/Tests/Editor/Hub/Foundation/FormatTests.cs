using System;
using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class FormatTests
    {
        private static readonly TimeSpan DeviceOffset = TimeSpan.FromHours(7);

        private static LiveOpsHubFormat CreateFormat()
        {
            return new LiveOpsHubFormat(DeviceOffset);
        }

        private static DateTime Utc(int year, int month, int day, int hour = 0, int minute = 0)
        {
            return new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc);
        }

        [Test]
        public void Duration_HoursAndMinutes_FullWords()
        {
            Assert.AreEqual("15 giờ 13 phút", CreateFormat().Duration(new TimeSpan(15, 13, 0), false));
        }

        [Test]
        public void Duration_WithDays_DropsMinutes()
        {
            Assert.AreEqual("3 ngày 18 giờ", CreateFormat().Duration(new TimeSpan(3, 18, 42, 0), false),
                "có ngày thì bỏ phút — '3 ngày 18 giờ 42 phút' quá dài cho chip và bảng [FD §6.1]");
        }

        [Test]
        public void Duration_Compact_ShortUnitsWithoutSpace()
        {
            Assert.AreEqual("15g 13p", CreateFormat().Duration(new TimeSpan(15, 13, 0), true));
        }

        [Test]
        public void Duration_ZeroParts_Omitted()
        {
            LiveOpsHubFormat format = CreateFormat();
            Assert.AreEqual("15 giờ", format.Duration(TimeSpan.FromHours(15), false));
            Assert.AreEqual("3 ngày", format.Duration(TimeSpan.FromDays(3), false));
            Assert.AreEqual("13 phút", format.Duration(TimeSpan.FromMinutes(13), false));
            Assert.AreEqual("0 phút", format.Duration(TimeSpan.FromSeconds(20), false));
        }

        /// <summary>
        /// (W9-24) Đơn vị thời lượng chia số ít/số nhiều ở bản TIẾNG ANH (luật Q-W5-2). Ba hàm <c>…UnitOf</c> là lối vào
        /// DUY NHẤT của ba đơn vị, nên đo chúng là đo cả ba chỗ gọi (<see cref="LiveOpsHubFormat.Duration"/>, meta làn của
        /// trục, readout lúc kéo). Bản tiếng Việt không chia số — hai vế phải ra y hệt nhau, đó cũng là một điều phải gác.
        /// </summary>
        [Test]
        public void DurationUnits_EnglishPicksSingularForExactlyOne()
        {
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                Assert.AreEqual("days", LiveOpsHubFormat.DayUnitOf(0L));
                Assert.AreEqual("day", LiveOpsHubFormat.DayUnitOf(1L));
                Assert.AreEqual("days", LiveOpsHubFormat.DayUnitOf(2L));
                Assert.AreEqual("hour", LiveOpsHubFormat.HourUnitOf(1L));
                Assert.AreEqual("hours", LiveOpsHubFormat.HourUnitOf(12L));
                Assert.AreEqual("minute", LiveOpsHubFormat.MinuteUnitOf(1L));
                Assert.AreEqual("minutes", LiveOpsHubFormat.MinuteUnitOf(0L));
            }

            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.Vietnamese))
            {
                Assert.AreEqual(LiveOpsHubFormat.DayUnitOf(2L), LiveOpsHubFormat.DayUnitOf(1L), "tiếng Việt không chia số");
                Assert.AreEqual(LiveOpsHubFormat.HourUnitOf(2L), LiveOpsHubFormat.HourUnitOf(1L), "tiếng Việt không chia số");
                Assert.AreEqual(LiveOpsHubFormat.MinuteUnitOf(2L), LiveOpsHubFormat.MinuteUnitOf(1L), "tiếng Việt không chia số");
            }
        }

        /// <summary>
        /// (W9-24) Đúng hai câu mà hành trình lượt 3 bắt được: ghi chú thời lượng của inspector in "= 1 days 12 hours", và
        /// readout lúc kéo in "shift +1 days". Đo tận chỗ sinh chữ chứ không chỉ đo đơn vị lẻ.
        /// </summary>
        [Test]
        public void Duration_And_DragLength_EnglishSingular()
        {
            using (LiveOpsHubLanguage.Override(LiveOpsHubLanguageId.English))
            {
                LiveOpsHubFormat format = CreateFormat();
                Assert.AreEqual("1 day 12 hours", format.Duration(TimeSpan.FromHours(36), false),
                    "ghi chú \"= …\" của inspector — bản cũ in \"1 days 12 hours\"");
                Assert.AreEqual("3 days 18 hours", format.Duration(new TimeSpan(3, 18, 42, 0), false));
                Assert.AreEqual("1 hour 1 minute", format.Duration(new TimeSpan(1, 1, 0), false));
                Assert.AreEqual("0 minutes", format.Duration(TimeSpan.FromSeconds(20), false), "không có \"0 minute\"");

                Assert.AreEqual("1 day", LiveOpsTimelineDragController.LengthText(TimeSpan.FromDays(1)),
                    "readout lúc kéo — bản cũ in \"1 days\"");
                Assert.AreEqual("3 days", LiveOpsTimelineDragController.LengthText(TimeSpan.FromDays(3)));
                Assert.AreEqual("1 hour", LiveOpsTimelineDragController.LengthText(TimeSpan.FromHours(1)));
                Assert.AreEqual("36 hours", LiveOpsTimelineDragController.LengthText(TimeSpan.FromHours(36)));
                Assert.AreEqual("1 hour 1 minute", LiveOpsTimelineDragController.LengthText(new TimeSpan(1, 1, 0)));
            }
        }

        [Test]
        public void Relative_FutureAndPast()
        {
            LiveOpsHubFormat format = CreateFormat();
            DateTime now = Utc(2026, 9, 13, 8, 47);
            Assert.AreEqual("sau 11 giờ 13 phút", format.Relative(now, Utc(2026, 9, 13, 20, 0)));
            Assert.AreEqual("13 giờ trước", format.Relative(now, Utc(2026, 9, 12, 19, 47)));
        }

        [Test]
        public void Remaining_UsesStillPrefix()
        {
            LiveOpsHubFormat format = CreateFormat();
            Assert.AreEqual("còn 19 ngày 15 giờ", format.Remaining(Utc(2026, 9, 13, 9, 0), Utc(2026, 10, 3, 0, 0)));
        }

        [Test]
        public void Numbers_VietnameseSeparators()
        {
            LiveOpsHubFormat format = CreateFormat();
            Assert.AreEqual("1.601 byte", format.Bytes(1601));
            Assert.AreEqual("48,4%", format.Percent(0.484));
            Assert.AreEqual("1.601", format.Integer(1601));
            Assert.AreEqual("1.234.567", format.Integer(1234567));
            Assert.AreEqual("12", format.Integer(12));
        }

        [Test]
        public void Dates_ShortWithoutLeadingZeros()
        {
            LiveOpsHubFormat format = CreateFormat();
            Assert.AreEqual("16/9 12:00", format.ShortDateTime(Utc(2026, 9, 16, 12, 0)));
            Assert.AreEqual("16/9 12:00 UTC", format.ShortDateTimeUtc(Utc(2026, 9, 16, 12, 0)));
            Assert.AreEqual("3/10/2026", format.DateWithYear(Utc(2026, 10, 3)));
            Assert.AreEqual("7/9 08:05", format.ShortDateTime(Utc(2026, 9, 7, 8, 5)), "'07/9' là cách viết cấm [FD §6.1]");
        }

        [Test]
        public void DeviceTime_PlusSeven()
        {
            LiveOpsHubFormat format = CreateFormat();
            Assert.AreEqual("19:00 16/9 giờ máy", format.DeviceTimeLine(Utc(2026, 9, 16, 12, 0)));
            Assert.AreEqual("07:00", format.DeviceClock(Utc(2026, 9, 18, 0, 0)));
            Assert.AreEqual("07:00 18/9 giờ máy", format.DeviceTimeLine(Utc(2026, 9, 18, 0, 0)));
            Assert.AreEqual("UTC+7", format.DeviceOffsetLabel);
        }

        [Test]
        public void DeviceOffsetLabel_ZeroAndFractional()
        {
            Assert.AreEqual("UTC", new LiveOpsHubFormat(TimeSpan.Zero).DeviceOffsetLabel);
            Assert.AreEqual("UTC+5:30", new LiveOpsHubFormat(new TimeSpan(5, 30, 0)).DeviceOffsetLabel);
            Assert.AreEqual("UTC-3", new LiveOpsHubFormat(TimeSpan.FromHours(-3)).DeviceOffsetLabel);
        }

        [Test]
        public void DayOfWeek_VietnameseNames()
        {
            LiveOpsHubFormat format = CreateFormat();
            Assert.AreEqual("thứ Hai", format.DayOfWeek(Utc(2026, 9, 14)));
            Assert.AreEqual("Chủ nhật", format.DayOfWeek(Utc(2026, 9, 13)));
        }

        [Test]
        public void IsoWeek_DesignSampleAndYearEdges()
        {
            Assert.AreEqual("Tuần 38", CreateFormat().IsoWeekLabel(Utc(2026, 9, 14)));
            // 1/1/2027 là thứ Sáu → thuộc tuần 53 của 2026 (2026 bắt đầu thứ Năm nên có 53 tuần).
            Assert.AreEqual(53, LiveOpsHubFormat.IsoWeekNumber(new DateTime(2027, 1, 1)));
            // 29/12/2025 là thứ Hai của tuần chứa thứ Năm 1/1/2026 → tuần 1 năm 2026.
            Assert.AreEqual(1, LiveOpsHubFormat.IsoWeekNumber(new DateTime(2025, 12, 29)));
            Assert.AreEqual(1, LiveOpsHubFormat.IsoWeekNumber(new DateTime(2026, 1, 1)));
        }

        [Test]
        public void Format_IndependentOfThreadCulture()
        {
            string[] cultureNames = { "vi-VN", "fr-FR", "en-US" };
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo originalUiCulture = Thread.CurrentThread.CurrentUICulture;
            string expected = null;
            try
            {
                foreach (string cultureName in cultureNames)
                {
                    CultureInfo culture = new CultureInfo(cultureName);
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;
                    string rendered = RenderEverything(CreateFormat());
                    if (expected == null)
                    {
                        expected = rendered;
                        continue;
                    }
                    Assert.AreEqual(expected, rendered,
                        "culture " + cultureName + " đổi chữ của hub — ảnh chụp và test sẽ lệch theo máy người dùng");
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUiCulture;
            }
            StringAssert.Contains("1.601 byte", expected);
            StringAssert.Contains("48,4%", expected);
        }

        private static string RenderEverything(LiveOpsHubFormat format)
        {
            DateTime moment = Utc(2026, 9, 16, 12, 0);
            return string.Join("|", new[]
            {
                format.ShortDateTime(moment), format.ShortDateTimeUtc(moment), format.DateWithYear(moment),
                format.DeviceTimeLine(moment), format.DeviceClock(moment), format.DeviceOffsetLabel,
                format.Duration(new TimeSpan(3, 18, 0, 0), false), format.Duration(new TimeSpan(15, 13, 0), true),
                format.Relative(moment, moment.AddHours(11.5)), format.Bytes(1601), format.Percent(0.484),
                format.Integer(1234567), format.DayOfWeek(moment), format.IsoWeekLabel(moment),
            });
        }
    }
}
