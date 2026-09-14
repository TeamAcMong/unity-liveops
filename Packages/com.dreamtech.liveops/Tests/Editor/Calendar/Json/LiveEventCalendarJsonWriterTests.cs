using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    [TestFixture]
    public sealed class LiveEventCalendarJsonWriterTests
    {
        private const string DesignSampleSha256Hex = "da4f831d840a21b7b4c08f9d7ffdb515f4ba89cdea7fae2ca8cde03e8d6c7646";

        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new UTF8Encoding(false);

        // ----- Dữ liệu mẫu: khoá từng byte (mục 5.6) -----

        [Test]
        public void DesignSample_Format2_MatchesDesignBytes()
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);

            byte[] expectedBytes = Utf8WithoutByteOrderMark.GetBytes(LiveOpsDesignSample.ExpectedFormat2Json);
            CollectionAssert.AreEqual(expectedBytes, json.GetUtf8Bytes());
            Assert.AreEqual(LiveOpsDesignSample.ExpectedFormat2Json, json.Text);
            Assert.AreEqual(1601, json.ByteCount);
            Assert.AreEqual(65, json.LineCount);
            Assert.AreEqual(65, json.Lines.Count);
            Assert.AreEqual(-1, json.Text.IndexOf('\r'), "Không CRLF — bản CRLF sẽ là 1.665 byte và sha khác.");
            Assert.IsFalse(json.Text.EndsWith("\n", StringComparison.Ordinal), "Không LF cuối file.");
            Assert.AreEqual(LiveEventCalendarJsonFormat.Version2, json.Format);
            Assert.AreEqual(DesignSampleSha256Hex, json.Sha256Hex);
            Assert.AreEqual("da4f83", json.ShortSha);
        }

        [Test]
        public void Format2_EmptyRecurring_WritesEmptyArrayOnOneLine()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "lava-a", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", ""))
                .Build();

            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);

            string expected =
                "{\n" +
                "  \"version\": 2,\n" +
                "  \"recurring\": [],\n" +
                "  \"events\": [\n" +
                "    {\n" +
                "      \"id\": \"lava-a\",\n" +
                "      \"type\": \"lava-quest\",\n" +
                "      \"startUtc\": \"2026-09-10T00:00:00Z\",\n" +
                "      \"endUtc\": \"2026-09-13T00:00:00Z\",\n" +
                "      \"configKey\": \"lava_quest_v2\"\n" +
                "    }\n" +
                "  ]\n" +
                "}";
            Assert.AreEqual(expected, json.Text);
        }

        [Test]
        public void EmptyDocument_BothFormats_WriteEmptyArrays()
        {
            Assert.AreEqual("{\n  \"version\": 2,\n  \"recurring\": [],\n  \"events\": []\n}",
                LiveEventCalendarJsonWriter.Write(LiveEventCalendarDocument.Empty, LiveEventCalendarJsonFormat.Version2).Text);
            Assert.AreEqual("{\n  \"events\": []\n}",
                LiveEventCalendarJsonWriter.Write(LiveEventCalendarDocument.Empty, LiveEventCalendarJsonFormat.Version1).Text);
        }

        [Test]
        public void UnreadableTime_WrittenVerbatim()
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);

            Assert.AreEqual("      \"endUtc\": \"2026-10-3\",", json.Lines[53].Text, "Dòng 54 giữ nguyên chuỗi hỏng — đúng cái game đọc.");

            LiveEventCalendarDocument broken = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "5/1/2026", "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "a", "lava-quest", "", " 2026-13-01 ", "k"))
                .Build();
            string text = LiveEventCalendarJsonWriter.Write(broken, LiveEventCalendarJsonFormat.Version2).Text;

            StringAssert.Contains("\"anchorUtc\": \"5/1/2026\",", text);
            StringAssert.Contains("\"startUtc\": \"\",", text);
            StringAssert.Contains("\"endUtc\": \" 2026-13-01 \",", text, "Không đọc được thì cả khoảng trắng cũng giữ nguyên văn.");
        }

        [Test]
        public void ReadableTime_WrittenCanonical()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T07:00+07:00", "", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "a", "lava-quest", "2026-09-10T00:00", "2026-09-13T00:00:00.000Z", "k"))
                .Build();

            string text = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;

            StringAssert.Contains("\"anchorUtc\": \"2026-01-05T00:00:00Z\",", text, "Múi +07:00 đổi về Z cùng thời điểm.");
            StringAssert.Contains("\"startUtc\": \"2026-09-10T00:00:00Z\",", text);
            StringAssert.Contains("\"endUtc\": \"2026-09-13T00:00:00Z\",", text);
        }

        [Test]
        public void ReadableTimeWithFractionalSeconds_KeepsFraction()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00.6Z", "", 24, 20, "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "a", "lava-quest", "2026-09-10T00:00:00.2Z", "2026-09-10T00:00:00.7Z", "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-b", "b", "lava-quest", "2026-09-11T07:30:00.1234567+07:00", "2026-09-12T00:00:00.500Z", "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-c", "c", "lava-quest", "2026-09-13T00:00:00.000Z", "2026-09-14T00:00:00.0000001Z", "k"))
                .Build();

            string text = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;

            StringAssert.Contains("\"anchorUtc\": \"2026-01-05T00:00:00.6Z\",", text, "Neo không được lùi 0,6 giây — cả dãy lần chạy sẽ lệch.");
            StringAssert.Contains("\"startUtc\": \"2026-09-10T00:00:00.2Z\",", text);
            StringAssert.Contains("\"endUtc\": \"2026-09-10T00:00:00.7Z\",", text);
            StringAssert.Contains("\"startUtc\": \"2026-09-11T00:30:00.1234567Z\",", text, "Múi đổi về Z, giữ đủ 7 chữ số lẻ.");
            StringAssert.Contains("\"endUtc\": \"2026-09-12T00:00:00.5Z\",", text, "Số 0 cuối của phần lẻ bỏ đi.");
            StringAssert.Contains("\"startUtc\": \"2026-09-13T00:00:00Z\",", text, "Phần lẻ toàn 0 là giờ tròn giây → dạng chuẩn.");
            StringAssert.Contains("\"endUtc\": \"2026-09-14T00:00:00.0000001Z\",", text, "Một tick cũng là thời điểm khác.");
        }

        [Test]
        public void FractionalSeconds_WrittenJsonCompilesLikeDraft_AndIsByteFixedPoint()
        {
            // Đúng ca V-6 (c): nháp giữ đợt (0,2 → 0,7 giây); nếu bộ ghi cắt phần lẻ thì JSON có bắt đầu = kết thúc và game bỏ đợt.
            LiveEventCalendarDocument draft = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00.6Z", "", 24, 20, "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "a", "lava-quest", "2026-09-10T00:00:00.2Z", "2026-09-10T00:00:00.7Z", "k"))
                .Build();
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(draft, LiveEventCalendarJsonFormat.Version2);

            // Dựng lại tài liệu từ đúng chuỗi giờ đã ghi (bộ đọc JSON của game ở RU, assembly test thuần không tham chiếu).
            LiveEventCalendarDocument fromWrittenJson = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race",
                    WrittenStringValue(json, LiveEventCalendarItemKind.RecurringRule, "sky-race", "anchorUtc"), "sky-race-", 24, 20, "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "a", "lava-quest",
                    WrittenStringValue(json, LiveEventCalendarItemKind.FixedEvent, "entry-a", "startUtc"),
                    WrittenStringValue(json, LiveEventCalendarItemKind.FixedEvent, "entry-a", "endUtc"), "k"))
                .Build();

            LiveEventCalendarCompilation draftCompilation = LiveEventCalendarCompiler.CompileInExportOrder(draft);
            LiveEventCalendarCompilation jsonCompilation = LiveEventCalendarCompiler.Compile(fromWrittenJson);
            Assert.AreEqual(draftCompilation.EntryCount, jsonCompilation.EntryCount);
            for (int index = 0; index < draftCompilation.EntryCount; index++)
            {
                LiveEventCalendarEntryOutcome draftOutcome = draftCompilation.Entries[index];
                LiveEventCalendarEntryOutcome jsonOutcome = jsonCompilation.Entries[index];
                Assert.AreEqual(draftOutcome.EventId, jsonOutcome.EventId, "mục " + index);
                Assert.AreEqual(draftOutcome.IsKept, jsonOutcome.IsKept, "mục " + index);
                Assert.AreEqual(draftOutcome.DropReason, jsonOutcome.DropReason, "mục " + index);
            }
            Assert.IsTrue(jsonCompilation.TryGetFixedOutcome("entry-a", out LiveEventCalendarEntryOutcome kept) && kept.IsKept,
                "Đợt 0,2 → 0,7 giây phải được game giữ như nháp.");

            DateTime fromUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime toUtc = new DateTime(2026, 1, 20, 0, 0, 0, DateTimeKind.Utc);
            IReadOnlyList<LiveEventInstance> draftInstances = draftCompilation.GetInstancesInRange("sky-race", fromUtc, toUtc);
            IReadOnlyList<LiveEventInstance> jsonInstances = jsonCompilation.GetInstancesInRange("sky-race", fromUtc, toUtc);
            Assert.AreEqual(draftInstances.Count, jsonInstances.Count);
            Assert.Greater(draftInstances.Count, 0);
            for (int index = 0; index < draftInstances.Count; index++)
            {
                Assert.AreEqual(draftInstances[index].StartUtc, jsonInstances[index].StartUtc, "lần chạy " + index);
            }

            CollectionAssert.AreEqual(json.GetUtf8Bytes(),
                LiveEventCalendarJsonWriter.Write(fromWrittenJson, LiveEventCalendarJsonFormat.Version2).GetUtf8Bytes(),
                "Ghi → đọc → ghi phải ra đúng byte cũ (mục 5.6 phép so 1).");
        }

        private static string WrittenStringValue(LiveEventCalendarJsonText json, LiveEventCalendarItemKind kind, string itemKey, string fieldName)
        {
            Assert.IsTrue(json.TryGetFieldLine(kind, itemKey, fieldName, out int lineNumber), fieldName);
            string lineText = json.Lines[lineNumber - 1].Text;
            const string separator = "\": \"";
            int valueStart = lineText.IndexOf(separator, StringComparison.Ordinal) + separator.Length;
            int valueEnd = lineText.LastIndexOf('"');
            return lineText.Substring(valueStart, valueEnd - valueStart);
        }

        [Test]
        public void MissingConfigKey_WritesTypeDefault()
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);
            Assert.IsTrue(json.TryGetFieldLine(LiveEventCalendarItemKind.FixedEvent, LiveOpsDesignSample.HuntEarlyEntryKey, "configKey",
                out int lineNumber));
            Assert.AreEqual("      \"configKey\": \"hunt_default\"", json.Lines[lineNumber - 1].Text, "hunt-0914 kế thừa hunt_default của loại.");

            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v4"))
                .WithEventType(new LiveEventTypeDefinition("empty-default", "Không mặc định", 2, false, ""))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 24, 20, ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-own", "own", "sky-race", "2026-09-01T00:00:00Z", "2026-09-02T00:00:00Z", "own_key"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-unknown", "unknown", "not-declared", "2026-09-03T00:00:00Z", "2026-09-04T00:00:00Z", ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-empty", "empty", "empty-default", "2026-09-05T00:00:00Z", "2026-09-06T00:00:00Z", ""))
                .Build();
            LiveEventCalendarJsonText written = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);

            Assert.AreEqual("      \"configKey\": \"sky_race_v4\"", LineOf(written, LiveEventCalendarItemKind.RecurringRule, "sky-race", "configKey"));
            Assert.AreEqual("      \"configKey\": \"own_key\"", LineOf(written, LiveEventCalendarItemKind.FixedEvent, "entry-own", "configKey"));
            Assert.AreEqual("      \"configKey\": \"\"", LineOf(written, LiveEventCalendarItemKind.FixedEvent, "entry-unknown", "configKey"),
                "Loại chưa khai: vẫn ghi configKey rỗng, không bỏ key.");
            Assert.AreEqual("      \"configKey\": \"\"", LineOf(written, LiveEventCalendarItemKind.FixedEvent, "entry-empty", "configKey"));
        }

        [Test]
        public void EmptyIdPrefix_WritesEffectivePrefix()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 24, 20, "sky_race_v4"))
                .Build();

            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);

            Assert.AreEqual("      \"idPrefix\": \"sky-race-\",", LineOf(json, LiveEventCalendarItemKind.RecurringRule, "sky-race", "idPrefix"));
        }

        [Test]
        public void Events_SortedByStartThenAssetOrder_UnreadableLast()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("entry-1", "late-first-in-asset", "t", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z", "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-2", "unreadable-first", "t", "2026-9-x", "2026-09-21T00:00:00Z", "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-3", "earliest", "t", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z", "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-4", "late-second-in-asset", "t", "2026-09-20T00:00:00Z", "2026-09-22T00:00:00Z", "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-5", "unreadable-second", "t", "", "", "k"))
                .Build();

            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);

            var writtenIds = new List<string>();
            var writtenItemKeys = new List<string>();
            foreach (LiveEventCalendarJsonLine line in json.Lines)
            {
                if (line.FieldName != "id") continue;
                writtenIds.Add(line.Text.Trim().Substring("\"id\": \"".Length).TrimEnd(',').TrimEnd('"'));
                writtenItemKeys.Add(line.ItemKey);
            }

            CollectionAssert.AreEqual(
                new[] { "earliest", "late-first-in-asset", "late-second-in-asset", "unreadable-first", "unreadable-second" }, writtenIds);
            CollectionAssert.AreEqual(new[] { "entry-3", "entry-1", "entry-4", "entry-2", "entry-5" }, writtenItemKeys,
                "Bản đồ dòng đi theo thứ tự xuất, không theo thứ tự asset.");
        }

        [Test]
        public void RecurringOrder_FollowsAssetOrder_NotEventTypeOrder()
        {
            // V-12: đưa làn lên/xuống (đổi thứ tự loại) không được đổi byte; đổi thứ tự luật trong asset thì có.
            LiveEventCalendarDocument sample = LiveOpsDesignSample.Document;
            var reversedTypes = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(sample.RemoteConfigKey);
            for (int index = sample.EventTypes.Count - 1; index >= 0; index--) reversedTypes.WithEventType(sample.EventTypes[index]);
            for (int index = 0; index < sample.RecurringRules.Count; index++) reversedTypes.WithRecurringRule(sample.RecurringRules[index]);
            for (int index = 0; index < sample.FixedEvents.Count; index++) reversedTypes.WithFixedEvent(sample.FixedEvents[index]);

            Assert.AreEqual(LiveOpsDesignSample.ExpectedFormat2Json,
                LiveEventCalendarJsonWriter.Write(reversedTypes.Build(), LiveEventCalendarJsonFormat.Version2).Text);

            LiveEventCalendarDocument skyRaceFirst = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(sample.RecurringRules[1])
                .WithRecurringRule(sample.RecurringRules[0])
                .Build();
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(skyRaceFirst, LiveEventCalendarJsonFormat.Version2);
            Assert.IsTrue(json.TryGetItemLineRange(LiveEventCalendarItemKind.RecurringRule, "sky-race", out int skyRaceFirstLine, out _));
            Assert.IsTrue(json.TryGetItemLineRange(LiveEventCalendarItemKind.RecurringRule, "weekly-pass", out int weeklyPassFirstLine, out _));
            Assert.Less(skyRaceFirstLine, weeklyPassFirstLine);
        }

        [Test]
        public void Escaping_QuoteBackslashControl_NonAsciiKept()
        {
            string vietnamese = "Nhi" + (char)0x1EC7 + "m v" + (char)0x1EE5;
            string rawId = "a\"b\\c\n\r\t" + (char)0x01 + (char)0x1F + " " + vietnamese;
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", rawId, "t", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z", "k"))
                .Build();

            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);

            string backslash = "\\";
            string expectedValue = "\"a" + backslash + "\"b" + backslash + backslash + "c" + backslash + "n" + backslash + "r" + backslash + "t" +
                                   backslash + "u0001" + backslash + "u001f " + vietnamese + "\"";
            Assert.AreEqual("      \"id\": " + expectedValue + ",", LineOf(json, LiveEventCalendarItemKind.FixedEvent, "entry-a", "id"));
            Assert.AreEqual(-1, json.Text.IndexOf(backslash + "u1ec7", StringComparison.OrdinalIgnoreCase), "Không ASCII giữ nguyên, không \\u.");
            Assert.AreEqual(json.Text.Split('\n').Length, json.LineCount, "LF trong giá trị đã escape — không tách thêm dòng.");

            byte[] bytes = json.GetUtf8Bytes();
            Assert.IsTrue(ContainsSequence(bytes, new byte[] { 0xE1, 0xBB, 0x87 }), "ệ ghi bằng đúng 3 byte UTF-8.");
            Assert.AreEqual(Utf8WithoutByteOrderMark.GetByteCount(json.Text), json.ByteCount);
            Assert.AreNotEqual(0xEF, bytes[0], "Không BOM.");
        }

        [Test]
        public void Format1_NoVersionNoRecurring()
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version1);

            // Mảng events của định dạng 1 trùng từng dòng với định dạng 2 (dòng 22 → hết) — chỉ gốc khác.
            string[] format2Lines = LiveOpsDesignSample.ExpectedFormat2Json.Split('\n');
            var expected = new StringBuilder("{\n  \"events\": [");
            for (int index = 21; index < format2Lines.Length; index++) expected.Append('\n').Append(format2Lines[index]);

            Assert.AreEqual(expected.ToString(), json.Text);
            Assert.AreEqual(LiveEventCalendarJsonFormat.Version1, json.Format);
            StringAssert.DoesNotContain("\"version\"", json.Text);
            StringAssert.DoesNotContain("\"recurring\"", json.Text);
            StringAssert.DoesNotContain("weekly-pass", json.Text);
            Assert.AreEqual(46, json.LineCount);
            Assert.IsTrue(json.TryGetFieldLine(LiveEventCalendarItemKind.FixedEvent, LiveOpsDesignSample.LavaQuestLateEntryKey, "endUtc", out int endLine));
            Assert.AreEqual(35, endLine, "Định dạng 1 lệch 19 dòng so với định dạng 2 (không version + recurring).");
            Assert.IsFalse(json.TryGetItemLineRange(LiveEventCalendarItemKind.RecurringRule, "weekly-pass", out _, out _));
        }

        // ----- Bản đồ dòng -----

        [Test]
        public void LineMap_FieldLinesPointToItems()
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);

            Assert.IsTrue(json.TryGetFieldLine(LiveEventCalendarItemKind.RecurringRule, "weekly-pass", "idPrefix", out int idPrefixLine));
            Assert.AreEqual(7, idPrefixLine);
            LiveEventCalendarJsonLine line7 = json.Lines[6];
            Assert.AreEqual(7, line7.Number);
            Assert.AreEqual("      \"idPrefix\": \"pass-\",", line7.Text);
            Assert.AreEqual(LiveEventCalendarItemKind.RecurringRule, line7.ItemKind);
            Assert.AreEqual("weekly-pass", line7.ItemKey);
            Assert.AreEqual("idPrefix", line7.FieldName);

            Assert.IsTrue(json.TryGetFieldLine(LiveEventCalendarItemKind.FixedEvent, LiveOpsDesignSample.LavaQuestLateEntryKey, "endUtc", out int endUtcLine));
            Assert.AreEqual(54, endUtcLine);
            LiveEventCalendarJsonLine line54 = json.Lines[53];
            Assert.AreEqual(LiveEventCalendarItemKind.FixedEvent, line54.ItemKind);
            Assert.AreEqual(LiveOpsDesignSample.LavaQuestLateEntryKey, line54.ItemKey);
            Assert.AreEqual("endUtc", line54.FieldName);

            Assert.IsTrue(json.TryGetItemLineRange(LiveEventCalendarItemKind.RecurringRule, "weekly-pass", out int weeklyFirst, out int weeklyLast));
            Assert.AreEqual((4, 11), (weeklyFirst, weeklyLast));
            Assert.IsTrue(json.TryGetItemLineRange(LiveEventCalendarItemKind.RecurringRule, "sky-race", out int skyFirst, out int skyLast));
            Assert.AreEqual((12, 19), (skyFirst, skyLast));
            Assert.IsTrue(json.TryGetItemLineRange(LiveEventCalendarItemKind.FixedEvent, LiveOpsDesignSample.LavaQuestLateEntryKey, out int lateFirst, out int lateLast));
            Assert.AreEqual((50, 56), (lateFirst, lateLast));
            Assert.IsTrue(json.TryGetItemLineRange(LiveEventCalendarItemKind.FixedEvent, LiveOpsDesignSample.StarTournamentEntryKey, out int starFirst, out int starLast));
            Assert.AreEqual((57, 63), (starFirst, starLast));
            Assert.AreEqual("", json.Lines[lateFirst - 1].FieldName, "Dòng { của mục có FieldName rỗng.");
            Assert.AreEqual("", json.Lines[lateLast - 1].FieldName, "Dòng } của mục có FieldName rỗng.");

            // Mỗi dòng thuộc đúng một chỗ: số dòng liên tục, dòng của gốc không mang mục.
            for (int index = 0; index < json.Lines.Count; index++) Assert.AreEqual(index + 1, json.Lines[index].Number);
            foreach (int rootLine in new[] { 1, 2, 3, 20, 21, 64, 65 })
            {
                Assert.IsNull(json.Lines[rootLine - 1].ItemKind, "Dòng " + rootLine + " thuộc gốc.");
                Assert.AreEqual("", json.Lines[rootLine - 1].ItemKey);
            }
            Assert.AreEqual("version", json.Lines[1].FieldName);
            Assert.AreEqual("events", json.Lines[20].FieldName);

            // Dòng thuộc mục nào thì nằm trong khoảng của mục đó.
            foreach (LiveEventCalendarJsonLine line in json.Lines)
            {
                if (!line.ItemKind.HasValue) continue;
                Assert.IsTrue(json.TryGetItemLineRange(line.ItemKind.Value, line.ItemKey, out int first, out int last));
                Assert.That(line.Number, Is.InRange(first, last));
            }

            Assert.IsFalse(json.TryGetFieldLine(LiveEventCalendarItemKind.FixedEvent, "not-a-key", "endUtc", out _));
            Assert.IsFalse(json.TryGetFieldLine(LiveEventCalendarItemKind.FixedEvent, LiveOpsDesignSample.LavaQuestLateEntryKey, "", out _));
            Assert.IsFalse(json.TryGetFieldLine(LiveEventCalendarItemKind.RecurringRule, LiveOpsDesignSample.LavaQuestLateEntryKey, "endUtc", out _),
                "Khoá mục phân biệt theo loại mục.");
            Assert.IsFalse(json.TryGetItemLineRange(LiveEventCalendarItemKind.EventType, "lava-quest", out _, out _),
                "Định nghĩa loại không đi vào JSON.");
            Assert.IsFalse(json.TryGetItemLineRange(LiveEventCalendarItemKind.FixedEvent, null, out _, out _));
        }

        [Test]
        public void LineMap_DuplicateRecurringType_PointsToFirstRule()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "first-", 24, 20, "k"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "second-", 24, 20, "k"))
                .Build();

            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);

            Assert.IsTrue(json.TryGetItemLineRange(LiveEventCalendarItemKind.RecurringRule, "sky-race", out int first, out int last));
            Assert.AreEqual((4, 11), (first, last), "Luật đứng trước thắng — cùng luật với tài liệu và bộ biên dịch.");
            Assert.AreEqual("      \"idPrefix\": \"first-\",", LineOf(json, LiveEventCalendarItemKind.RecurringRule, "sky-race", "idPrefix"));
        }

        // ----- Sha, byte, culture -----

        [Test]
        public void Sha_IsStableAcrossCalls()
        {
            LiveEventCalendarJsonText firstWrite = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);
            LiveEventCalendarJsonText secondWrite = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);

            Assert.AreEqual(firstWrite.Sha256Hex, secondWrite.Sha256Hex);
            Assert.AreEqual(DesignSampleSha256Hex, firstWrite.Sha256Hex);
            Assert.AreEqual(LiveEventCalendarSha256.ComputeHex(firstWrite.GetUtf8Bytes()), firstWrite.Sha256Hex);
            Assert.AreEqual(firstWrite.Sha256Hex.Substring(0, 6), firstWrite.ShortSha);

            // Bên gọi sửa mảng đã lấy không làm lệch byte/sha của lần lấy sau.
            byte[] bytes = firstWrite.GetUtf8Bytes();
            bytes[0] = (byte)'X';
            Assert.AreEqual((byte)'{', firstWrite.GetUtf8Bytes()[0]);
            Assert.AreEqual(DesignSampleSha256Hex, firstWrite.Sha256Hex);

            string format1Sha = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version1).Sha256Hex;
            Assert.AreNotEqual(DesignSampleSha256Hex, format1Sha, "Hai định dạng là hai chuỗi byte khác nhau.");
        }

        [Test]
        public void Write_IndependentOfThreadCulture()
        {
            // Số âm và giờ có múi đi qua đúng các chỗ culture có thể chen vào (dấu âm, chữ số, định dạng ngày).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v4"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T07:30+07:00", "", -24, -1, ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "Istanbul-i", "sky-race", "2026-09-10T00:00:00Z", "2026-9-11", ""))
                .WithFixedEvent(new FixedLiveEventEntry("entry-b", "fraction", "sky-race", "2026-09-12T07:30:00.25Z", "2026-09-13T00:00:00Z", ""))
                .Build();

            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            CultureInfo originalUserInterfaceCulture = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                byte[] invariantBytes = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).GetUtf8Bytes();
                byte[] invariantSampleBytes = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2).GetUtf8Bytes();
                StringAssert.Contains("\"periodHours\": -24,", Utf8WithoutByteOrderMark.GetString(invariantBytes));

                // vi-VN/fr-FR/en-US/tr-TR là culture thật của người dùng, nhưng dấu âm và dấu phân cách giờ của cả bốn trùng
                // Invariant (đo trên Mono 2022.3 và .NET 9) — riêng chúng không bắt được việc bỏ InvariantCulture. Culture
                // đối nghịch tất định (không phụ thuộc dữ liệu ICU/Mono của máy) mới làm test đỏ khi bỏ InvariantCulture ở
                // FormatInteger hoặc ở định dạng giờ.
                CultureInfo adversarialCulture = CreateAdversarialCulture();
                Assert.AreEqual("~24", (-24).ToString(adversarialCulture), "Culture đối nghịch phải thật sự đổi dấu âm.");
                // Cố ý định dạng theo culture đối nghịch (không Invariant): đây là phép đo chính culture đó, không phải đường ghi.
                const string timeOfDayFormat = "HH:mm:ss";
                Assert.AreEqual("07.30.00", new DateTime(2026, 1, 5, 7, 30, 0).ToString(timeOfDayFormat, adversarialCulture),
                    "Culture đối nghịch phải thật sự đổi dấu phân cách giờ.");

                var cultures = new List<CultureInfo>();
                foreach (string cultureName in new[] { "vi-VN", "fr-FR", "en-US", "tr-TR" }) cultures.Add(new CultureInfo(cultureName));
                cultures.Add(adversarialCulture);

                foreach (CultureInfo culture in cultures)
                {
                    string cultureName = culture.Name.Length > 0 ? culture.Name : "adversarial-invariant-clone";
                    Thread.CurrentThread.CurrentCulture = culture;
                    Thread.CurrentThread.CurrentUICulture = culture;

                    CollectionAssert.AreEqual(invariantBytes, LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).GetUtf8Bytes(),
                        cultureName);
                    CollectionAssert.AreEqual(invariantSampleBytes,
                        LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2).GetUtf8Bytes(), cultureName);
                    Assert.AreEqual(DesignSampleSha256Hex,
                        LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2).Sha256Hex, cultureName);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
                Thread.CurrentThread.CurrentUICulture = originalUserInterfaceCulture;
            }
        }

        /// <summary>
        /// Bản sao Invariant với dấu âm "~" và dấu phân cách giờ "." — vì sao: ':' trong chuỗi định dạng giờ và dấu âm của
        /// int.ToString() đều lấy từ culture; một đường ghi quên InvariantCulture sẽ ra "~24" / "07.30.00Z" và lệch byte.
        /// </summary>
        internal static CultureInfo CreateAdversarialCulture()
        {
            CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            culture.NumberFormat.NegativeSign = "~";
            culture.DateTimeFormat.TimeSeparator = ".";
            return culture;
        }

        // ----- Một dòng, object đứng riêng -----

        [Test]
        public void WriteSingleLine_DesignSample_NoWhitespaceOutsideStrings()
        {
            LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version2);

            string singleLine = LiveEventCalendarJsonWriter.WriteSingleLine(json);

            Assert.AreEqual(1166, Utf8WithoutByteOrderMark.GetByteCount(singleLine), "Đúng 1.166 byte của bản một dòng [SD2 §4 mục 5].");
            const string expectedStart = "{\"version\":2,\"recurring\":[{\"type\":\"weekly-pass\",\"anchorUtc\":\"2026-01-05T00:00:00Z\",";
            Assert.IsTrue(singleLine.StartsWith(expectedStart, StringComparison.Ordinal), singleLine);
            Assert.IsTrue(singleLine.EndsWith("\"configKey\":\"star_tournament_v1\"}]}", StringComparison.Ordinal), singleLine);
            Assert.AreEqual(-1, singleLine.IndexOf('\n'));
            Assert.AreEqual(DesignSampleSha256Hex, json.Sha256Hex, "Xem một dòng không đổi sha của bản đã định dạng (PD-12).");
        }

        [Test]
        public void WriteSingleLine_KeepsWhitespaceAndEscapedQuotesInsideStrings()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("entry-a", "a\" b \\", "t", "not a date", "2026-09-11T00:00:00Z", "key with space"))
                .Build();

            string singleLine = LiveEventCalendarJsonWriter.WriteSingleLine(LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version1));

            Assert.AreEqual(
                "{\"events\":[{\"id\":\"a\\\" b \\\\\",\"type\":\"t\",\"startUtc\":\"not a date\",\"endUtc\":\"2026-09-11T00:00:00Z\",\"configKey\":\"key with space\"}]}",
                singleLine);
        }

        [Test]
        public void WriteFixedEventObject_SameRulesAsEventsArray()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            Assert.IsTrue(document.TryGetFixedEvent(LiveOpsDesignSample.HuntEarlyEntryKey, out FixedLiveEventEntry huntEarly));
            Assert.IsTrue(document.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestLateEntryKey, out FixedLiveEventEntry lavaLate));

            Assert.AreEqual(
                "{\n" +
                "  \"id\": \"hunt-0914\",\n" +
                "  \"type\": \"treasure-hunt\",\n" +
                "  \"startUtc\": \"2026-09-14T00:00:00Z\",\n" +
                "  \"endUtc\": \"2026-09-17T00:00:00Z\",\n" +
                "  \"configKey\": \"hunt_default\"\n" +
                "}",
                LiveEventCalendarJsonWriter.WriteFixedEventObject(document, huntEarly));
            StringAssert.Contains("  \"endUtc\": \"2026-10-3\",\n", LiveEventCalendarJsonWriter.WriteFixedEventObject(document, lavaLate));
        }

        [Test]
        public void WriteRecurringRuleObject_SameRulesAsRecurringArray()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            Assert.IsTrue(document.TryGetRecurringRule("weekly-pass", out RecurringLiveEventRule weeklyPass));

            Assert.AreEqual(
                "{\n" +
                "  \"type\": \"weekly-pass\",\n" +
                "  \"anchorUtc\": \"2026-01-05T00:00:00Z\",\n" +
                "  \"idPrefix\": \"pass-\",\n" +
                "  \"periodHours\": 168,\n" +
                "  \"activeHours\": 168,\n" +
                "  \"configKey\": \"weekly_pass_s3\"\n" +
                "}",
                LiveEventCalendarJsonWriter.WriteRecurringRuleObject(document, weeklyPass));

            var inheritsDefault = new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "", 24, 20, "");
            string inherited = LiveEventCalendarJsonWriter.WriteRecurringRuleObject(document, inheritsDefault);
            StringAssert.Contains("  \"idPrefix\": \"sky-race-\",\n", inherited);
            StringAssert.Contains("  \"configKey\": \"sky_race_v4\"\n", inherited);
        }

        [Test]
        public void NullOrInvalidArguments_Throw()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;

            Assert.Throws<ArgumentNullException>(() => LiveEventCalendarJsonWriter.Write(null, LiveEventCalendarJsonFormat.Version2));
            Assert.Throws<ArgumentOutOfRangeException>(() => LiveEventCalendarJsonWriter.Write(document, (LiveEventCalendarJsonFormat)3));
            Assert.Throws<ArgumentNullException>(() => LiveEventCalendarJsonWriter.WriteSingleLine(null));
            Assert.Throws<ArgumentNullException>(() => LiveEventCalendarJsonWriter.WriteFixedEventObject(document, null));
            Assert.Throws<ArgumentNullException>(() => LiveEventCalendarJsonWriter.WriteFixedEventObject(null, document.FixedEvents[0]));
            Assert.Throws<ArgumentNullException>(() => LiveEventCalendarJsonWriter.WriteRecurringRuleObject(document, null));
            Assert.Throws<ArgumentNullException>(() => LiveEventCalendarJsonWriter.WriteRecurringRuleObject(null, document.RecurringRules[0]));
        }

        [Test]
        public void Write_DoesNotModifyDocumentOrder()
        {
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("entry-late", "late", "t", "2026-09-20T00:00:00Z", "2026-09-21T00:00:00Z", "k"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-early", "early", "t", "2026-09-10T00:00:00Z", "2026-09-11T00:00:00Z", "k"))
                .Build();

            LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);

            Assert.AreEqual("entry-late", document.FixedEvents[0].EntryKey, "Bộ ghi sắp trên bản sao, tài liệu nháp giữ thứ tự asset.");
        }

        private static string LineOf(LiveEventCalendarJsonText json, LiveEventCalendarItemKind kind, string itemKey, string fieldName)
        {
            Assert.IsTrue(json.TryGetFieldLine(kind, itemKey, fieldName, out int lineNumber), itemKey + "." + fieldName);
            return json.Lines[lineNumber - 1].Text;
        }

        private static bool ContainsSequence(byte[] haystack, byte[] needle)
        {
            for (int start = 0; start + needle.Length <= haystack.Length; start++)
            {
                bool matches = true;
                for (int offset = 0; offset < needle.Length && matches; offset++) matches = haystack[start + offset] == needle[offset];
                if (matches) return true;
            }
            return false;
        }
    }
}
