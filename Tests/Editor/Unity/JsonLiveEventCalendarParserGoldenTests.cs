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

            // Mục mang HAI lỗi: 0.1.0 chỉ báo lỗi kiểm trước (startUtc → endUtc → id → loại → end <= start). Bộ biên dịch mới
            // chọn lý do chính theo đúng thứ tự này (V-7 "khớp 0.1.0, khoá bằng golden") — nếu đảo thứ tự kiểm thì câu Problems
            // của mục nhiều lỗi đổi dù từng lỗi riêng lẻ vẫn ra đúng câu. Gộp vào test này thay vì thêm test để giữ đúng số 7
            // test golden / 13 test Unity của nghiệm thu G-GOLDEN.
            const string multiFaultJson = "{\"events\":[" +
                                           "{\"type\":\"hunt\",\"startUtc\":\"14/09/2026\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                           "{\"id\":\"both-dates\",\"type\":\"hunt\",\"startUtc\":\"hôm nay\",\"endUtc\":\"mai\"}," +
                                           "{\"id\":\"bad-end\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"mai\"}," +
                                           "{\"id\":\"a#b\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"mai\"}," +
                                           "{\"type\":\"hunt\",\"startUtc\":\"2026-09-25T00:00:00Z\",\"endUtc\":\"2026-09-24T00:00:00Z\"}," +
                                           "{\"id\":\"no-type\",\"startUtc\":\"2026-09-25T00:00:00Z\",\"endUtc\":\"2026-09-24T00:00:00Z\"}," +
                                           "{\"id\":\"a#b\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                           "{\"id\":\"typed\",\"type\":\"hunt#x\",\"startUtc\":\"2026-09-25T00:00:00Z\",\"endUtc\":\"2026-09-24T00:00:00Z\"}," +
                                           "{\"id\":\"line\\nbreak\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\"}" +
                                           "]}";

            LiveEventCalendarParseResult multiFaultResult = JsonLiveEventCalendarParser.Parse(multiFaultJson);

            Assert.AreEqual(0, multiFaultResult.Calendar.Instances.Count, "Cả 9 mục đều hỏng.");
            Assert.AreEqual(9, multiFaultResult.Problems.Count, string.Join("\n", multiFaultResult.Problems));

            // Thiếu id + startUtc hỏng → giờ thắng; id thiếu vẫn in trong ngoặc là ''.
            Assert.AreEqual("Mục thứ 1 (''): startUtc không phải giờ ISO 8601: '14/09/2026' — bỏ qua.", multiFaultResult.Problems[0]);
            // Hỏng cả hai giờ → chỉ báo startUtc.
            Assert.AreEqual("Mục thứ 2 ('both-dates'): startUtc không phải giờ ISO 8601: 'hôm nay' — bỏ qua.", multiFaultResult.Problems[1]);
            // Nhánh endUtc hỏng (không test nào khác chạm).
            Assert.AreEqual("Mục thứ 3 ('bad-end'): endUtc không phải giờ ISO 8601: 'mai' — bỏ qua.", multiFaultResult.Problems[2]);
            // Id chứa '#' + endUtc hỏng → giờ thắng id.
            Assert.AreEqual("Mục thứ 4 ('a#b'): endUtc không phải giờ ISO 8601: 'mai' — bỏ qua.", multiFaultResult.Problems[3]);
            // Thiếu id + end < start → id thắng giờ ngược.
            Assert.AreEqual("Mục thứ 5: Event id không được rỗng. — bỏ qua.",
                             WithoutRuntimeParameterSuffix(multiFaultResult.Problems[4]));
            // Thiếu loại + end < start → loại thắng giờ ngược.
            Assert.AreEqual("Mục thứ 6: Loại event không được rỗng. — bỏ qua.",
                             WithoutRuntimeParameterSuffix(multiFaultResult.Problems[5]));
            // Id chứa '#' + thiếu loại → id thắng loại.
            Assert.AreEqual("Mục thứ 7: Event id không được chứa '#' hay xuống dòng: a#b — bỏ qua.",
                             WithoutRuntimeParameterSuffix(multiFaultResult.Problems[6]));
            // Loại chứa '#' + end < start → loại thắng giờ ngược.
            Assert.AreEqual("Mục thứ 8: Loại event không được chứa '#' hay xuống dòng: hunt#x — bỏ qua.",
                             WithoutRuntimeParameterSuffix(multiFaultResult.Problems[7]));
            // Xuống dòng trong id: câu in nguyên ký tự xuống dòng của id.
            Assert.AreEqual("Mục thứ 9: Event id không được chứa '#' hay xuống dòng: line\nbreak — bỏ qua.",
                             WithoutRuntimeParameterSuffix(multiFaultResult.Problems[8]));
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

            // Bẫy vị trí của 0.1.0: câu trùng id đánh số trong danh sách ĐÃ LỌC mục hỏng (FixedLiveEventCalendar chỉ thấy
            // các instance parser dựng được), không phải vị trí trong JSON. Ở đây 'a' thứ hai là mục JSON thứ 3 nhưng câu nói
            // "mục thứ 2". Bộ biên dịch mới đánh SourceIndex theo JSON — nếu dùng thẳng SourceIndex + 1 cho ProblemText thì
            // câu đổi; khoá lại để G-UNITY phải giữ cách đánh số cũ trong câu Problems.
            const string brokenBeforeDuplicateJson = "{\"events\":[" +
                                                      "{\"id\":\"bad-date\",\"type\":\"hunt\",\"startUtc\":\"14/09/2026\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                                      "{\"id\":\"a\",\"type\":\"hunt\",\"startUtc\":\"2026-09-14T00:00:00Z\",\"endUtc\":\"2026-09-15T00:00:00Z\"}," +
                                                      "{\"id\":\"a\",\"type\":\"hunt\",\"startUtc\":\"2026-09-16T00:00:00Z\",\"endUtc\":\"2026-09-17T00:00:00Z\"}" +
                                                      "]}";

            LiveEventCalendarParseResult brokenBeforeDuplicateResult = JsonLiveEventCalendarParser.Parse(brokenBeforeDuplicateJson);

            Assert.AreEqual(1, brokenBeforeDuplicateResult.Calendar.Instances.Count);
            Assert.AreEqual("a", brokenBeforeDuplicateResult.Calendar.Instances[0].EventId);
            Assert.AreEqual(2, brokenBeforeDuplicateResult.Problems.Count, string.Join("\n", brokenBeforeDuplicateResult.Problems));
            Assert.AreEqual("Mục thứ 1 ('bad-date'): startUtc không phải giờ ISO 8601: '14/09/2026' — bỏ qua.",
                             brokenBeforeDuplicateResult.Problems[0]);
            Assert.AreEqual("Trùng id 'a' ở mục thứ 2 — giữ mục xuất hiện trước.", brokenBeforeDuplicateResult.Problems[1]);
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
