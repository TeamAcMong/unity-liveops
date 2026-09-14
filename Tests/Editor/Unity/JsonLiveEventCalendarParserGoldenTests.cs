using System;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary>
    /// Khoá chuỗi <c>Problems</c> của <see cref="JsonLiveEventCalendarParser"/> bản 0.1.0 TRƯỚC khi P1 viết lại parser để
    /// đi qua bộ biên dịch tài liệu mới (G-UNITY). Gói viết lại phải giữ các test này xanh — đổi câu Problems ở đây là đổi
    /// hành vi remote config đang chạy thật, không phải refactor nội bộ. Không sửa file này sau W0 (G-UNITY chỉ được thêm
    /// fixture mới, không sửa fixture này).
    /// </summary>
    [TestFixture]
    public sealed class JsonLiveEventCalendarParserGoldenTests
    {
        // .NET/Mono ghép thêm đuôi tên tham số vào ArgumentException.Message khác nhau giữa runtime (2022.3 Mono kiểu cũ
        // "\r\nParameter name: x", 6000.6 kiểu mới " (Parameter 'x')") — chuỗi đó không phải câu Problems của game, bỏ đi
        // trước khi so để hai bản Unity cùng khoá được một chuỗi tiếng Việt.
        private static readonly Regex RuntimeParameterSuffix =
            new Regex(@"(\r?\nParameter name: \w+)|( \(Parameter '\w+'\))", RegexOptions.Compiled);

        private static string WithoutRuntimeParameterSuffix(string problem)
        {
            return RuntimeParameterSuffix.Replace(problem, string.Empty);
        }

        [Test]
        public void Format1_BrokenEntries_ProblemTextsUnchanged()
        {
            const string json = "{\"events\":[" +
                                 "{\"id\":\"ok\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                 "{\"type\":\"hunt\",\"startUtc\":\"2026-09-20T00:00:00Z\",\"endUtc\":\"2026-09-21T00:00:00Z\"}," +
                                 "{\"id\":\"bad-date\",\"type\":\"hunt\",\"startUtc\":\"14/09/2026\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                 "{\"id\":\"backwards\",\"type\":\"hunt\",\"startUtc\":\"2026-09-25T00:00:00Z\",\"endUtc\":\"2026-09-24T00:00:00Z\"}," +
                                 "{\"id\":\"overlap\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T12:00:00Z\",\"endUtc\":\"2026-09-16T00:00:00Z\"}" +
                                 "]}";

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

            Assert.AreEqual(1, result.Calendar.Instances.Count, "Chỉ 'ok' hợp lệ, 4 mục còn lại phải bị bỏ.");
            Assert.AreEqual("ok", result.Calendar.Instances[0].EventId);
            Assert.AreEqual(4, result.Problems.Count, string.Join("\n", result.Problems));

            Assert.AreEqual("Mục thứ 2: Event id không được rỗng. — bỏ qua.", WithoutRuntimeParameterSuffix(result.Problems[0]));
            Assert.AreEqual("Mục thứ 3 ('bad-date'): startUtc không phải giờ ISO 8601: '14/09/2026' — bỏ qua.", result.Problems[1]);
            Assert.AreEqual("Mục thứ 4: Đợt event phải kết thúc sau khi bắt đầu: backwards — bỏ qua.",
                             WithoutRuntimeParameterSuffix(result.Problems[2]));
            Assert.AreEqual("Đợt 'overlap' chồng giờ với 'ok' cùng loại 'hunt' — bỏ 'overlap'.", result.Problems[3]);
        }

        [Test]
        public void BrokenJson_ProblemPrefixUnchanged()
        {
            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse("{ not json");

            Assert.AreEqual(0, result.Calendar.Instances.Count);
            Assert.AreEqual(1, result.Problems.Count, string.Join("\n", result.Problems));
            // Chỉ khoá tiền tố: phần sau ": " là exception.Message của JsonUtility, đổi theo phiên bản Unity.
            StringAssert.StartsWith("JSON lịch event hỏng: ", result.Problems[0]);
        }

        [Test]
        public void EmptyObject_MissingEventsProblemUnchanged()
        {
            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse("{}");

            Assert.AreEqual(0, result.Calendar.Instances.Count);
            Assert.AreEqual(1, result.Problems.Count, string.Join("\n", result.Problems));
            Assert.AreEqual("JSON lịch event thiếu mảng \"events\".", result.Problems[0]);
        }

        [Test]
        public void Blank_NoProblem()
        {
            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse("  ");

            Assert.IsFalse(result.HasProblems, "Remote config chưa có key = chưa có event, không phải lỗi.");
            Assert.AreEqual(0, result.Calendar.Instances.Count);
            Assert.AreSame(FixedLiveEventCalendar.Empty, result.Calendar);
        }

        [Test]
        public void DuplicateAndOverlap_ProblemTextsAndOrderUnchanged()
        {
            const string json = "{\"events\":[" +
                                 "{\"id\":\"a\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                 "{\"id\":\"a\",\"type\":\"hunt\",\"startUtc\":\"2026-09-16T00:00:00Z\",\"endUtc\":\"2026-09-17T00:00:00Z\"}," +
                                 "{\"id\":\"b\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T12:00:00Z\",\"endUtc\":\"2026-09-16T00:00:00Z\"}" +
                                 "]}";

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

            Assert.AreEqual(1, result.Calendar.Instances.Count, "Chỉ 'a' xuất hiện đầu tiên được giữ.");
            Assert.AreEqual("a", result.Calendar.Instances[0].EventId);
            Assert.AreEqual(2, result.Problems.Count, string.Join("\n", result.Problems));
            // Trùng id bị loại TRƯỚC khi xét chồng giờ (LiveEventCalendar dựng candidates rồi mới sort+overlap) — thứ tự này là hành vi khoá.
            Assert.AreEqual("Trùng id 'a' ở mục thứ 2 — giữ mục xuất hiện trước.", result.Problems[0]);
            Assert.AreEqual("Đợt 'b' chồng giờ với 'a' cùng loại 'hunt' — bỏ 'b'.", result.Problems[1]);
        }

        [Test]
        public void OffsetTime_ConvertedToUtc()
        {
            const string json = "{\"events\":[" +
                                 "{\"id\":\"hunt-001\",\"type\":\"treasure-hunt\",\"startUtc\":\"2026-09-15T07:00:00+07:00\",\"endUtc\":\"2026-09-16T00:00:00\"}" +
                                 "]}";

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

            Assert.IsFalse(result.HasProblems, string.Join("\n", result.Problems));
            Assert.AreEqual(1, result.Calendar.Instances.Count);
            LiveEventInstance hunt = result.Calendar.Instances[0];
            Assert.AreEqual(new DateTime(2026, 9, 15, 0, 0, 0, DateTimeKind.Utc), hunt.StartUtc, "+07:00 phải đổi về UTC.");
            Assert.AreEqual(new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc), hunt.EndUtc, "Không ghi múi giờ nghĩa là UTC.");
        }

        [Test]
        public void DemoJson_OneProblem()
        {
            // Nhại đúng hình dạng JSON của Assets/Demo/Scripts/LiveOpsDemo.BuildHuntCalendarJson lúc khoá golden này (một
            // mục hợp lệ + một mục cố ý hỏng "ngày mai" để panel demo có gì đó liệt kê) — giờ đóng băng, KHÔNG gọi thẳng
            // LiveOpsDemo vì Demo nằm ngoài package, test package không được tham chiếu Assets/Demo.
            const string json = "{\"events\":[" +
                                 "{\"id\":\"hunt-20260914\",\"type\":\"treasure-hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\"," +
                                 "\"endUtc\":\"2026-09-17T00:00:00Z\",\"configKey\":\"hunt_demo_v1\"}," +
                                 "{\"id\":\"hunt-broken\",\"type\":\"treasure-hunt\",\"startUtc\":\"ngày mai\",\"endUtc\":\"2026-01-01T00:00:00Z\"}" +
                                 "]}";

            LiveEventCalendarParseResult result = JsonLiveEventCalendarParser.Parse(json);

            Assert.AreEqual(1, result.Calendar.Instances.Count);
            Assert.AreEqual("hunt-20260914", result.Calendar.Instances[0].EventId);
            Assert.AreEqual(1, result.Problems.Count, string.Join("\n", result.Problems));
            Assert.AreEqual("Mục thứ 2 ('hunt-broken'): startUtc không phải giờ ISO 8601: 'ngày mai' — bỏ qua.", result.Problems[0]);
        }
    }
}
