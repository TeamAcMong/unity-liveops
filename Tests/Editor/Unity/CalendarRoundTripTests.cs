using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary>
    /// Khứ hồi asset ↔ tài liệu ↔ JSON ↔ parser game (mục 5.6, V-4, V-6, V-9). Ba phép so khứ hồi JSON — điểm bất động theo
    /// byte, tương đương JSON (<see cref="LiveOpsDocumentAssert.AreJsonEquivalent"/>), tương đương đã biên dịch — chạy trên mẫu
    /// thiết kế và trên 200 tài liệu ngẫu nhiên seed cố định (<see cref="LiveOpsRandomDocuments"/>). Không test nào ở đây so tài
    /// liệu đọc lại bằng field thô: bộ ghi chủ ý làm mất thông tin (thứ tự asset, configKey kế thừa, tiền tố rỗng, dạng giờ,
    /// EntryKey, loại, dấu, cảnh báo), nên so thô chỉ đo được "bộ ghi có làm đúng việc của nó không" — sai câu hỏi.
    /// </summary>
    [TestFixture]
    public sealed class CalendarRoundTripTests
    {
        private static readonly LiveEventCalendarJsonFormat[] Formats = { LiveEventCalendarJsonFormat.Version1, LiveEventCalendarJsonFormat.Version2 };

        // "1 năm quanh neo mẫu" (5.6 phép so 3): đủ phủ mọi đợt bộ sinh rải (20/8 → ~20/12/2026) và nhiều chu kỳ của mọi luật.
        private static readonly DateTime InstanceWindowStartUtc = LiveOpsDesignSample.NowUtc.AddDays(-183);
        private static readonly DateTime InstanceWindowEndUtc = LiveOpsDesignSample.NowUtc.AddDays(183);

        private static readonly HashSet<string> RulesOneToSeven = new HashSet<string>(StringComparer.Ordinal)
        {
            LiveEventCalendarRuleIds.UtcTimeFormat,
            LiveEventCalendarRuleIds.EndBeforeStart,
            LiveEventCalendarRuleIds.InvalidIdentifier,
            LiveEventCalendarRuleIds.DuplicateEventId,
            LiveEventCalendarRuleIds.OverlapSameType,
            LiveEventCalendarRuleIds.RecurringRuleInvalid,
            LiveEventCalendarRuleIds.ShadowedByRecurring,
        };

        private readonly List<LiveEventCalendarAsset> _createdAssets = new List<LiveEventCalendarAsset>();

        [TearDown]
        public void TearDown()
        {
            for (int index = 0; index < _createdAssets.Count; index++)
            {
                if (_createdAssets[index] != null) UnityEngine.Object.DestroyImmediate(_createdAssets[index]);
            }
            _createdAssets.Clear();
        }

        private LiveEventCalendarAsset CreateAsset()
        {
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            _createdAssets.Add(asset);
            return asset;
        }

        // ----- Asset ↔ tài liệu (không mất thông tin: so chính xác) -----

        [Test]
        public void Asset_ToDocument_ApplyDocument_RoundTrip()
        {
            LiveEventCalendarAsset asset = CreateAsset();
            AssertSupport("Mẫu thiết kế", LiveOpsDesignSample.Document, () =>
            {
                asset.ApplyDocument(LiveOpsDesignSample.Document);
                LiveOpsDocumentAssert.AssertDocumentsEqual(LiveOpsDesignSample.Document, asset.ToDocument());
            });

            // Cùng MỘT asset qua 200 tài liệu: ApplyDocument phải THAY toàn bộ danh sách, không để sót mục của tài liệu trước.
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                AssertSupport(LiveOpsRandomDocuments.Label(documentIndex), document, () =>
                {
                    asset.ApplyDocument(document);
                    LiveEventCalendarDocument roundTripped = asset.ToDocument();
                    LiveOpsDocumentAssert.AssertDocumentsEqual(document, roundTripped);
                    // Gọi ToDocument lần hai ra cùng tài liệu: Undo/postprocessor đọc lại nhiều lần không được sinh "chưa lưu" giả.
                    LiveOpsDocumentAssert.AssertDocumentsEqual(roundTripped, asset.ToDocument());
                });
            }
        }

        // ----- Ba phép so khứ hồi JSON (V-4) trên mẫu thiết kế -----

        [Test]
        public void DesignSample_WriteParseWrite_IsByteFixedPoint_BothFormats()
        {
            for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
            {
                AssertByteFixedPoint("Mẫu thiết kế", LiveOpsDesignSample.Document, Formats[formatIndex]);
            }
        }

        [Test]
        public void DesignSample_ParsedDocument_IsJsonEquivalent_BothFormats()
        {
            for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
            {
                AssertJsonEquivalentAfterParse("Mẫu thiết kế", LiveOpsDesignSample.Document, Formats[formatIndex]);
            }
        }

        [Test]
        public void DesignSample_ParsedDocument_CompiledEquivalent_BothFormats()
        {
            for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
            {
                AssertCompiledEquivalentAfterParse("Mẫu thiết kế", LiveOpsDesignSample.Document, Formats[formatIndex]);
            }
        }

        [Test]
        public void Document_WriteParseDocument_RoundTrip_PreservesBrokenStrings()
        {
            // Mẫu + mọi dạng chuỗi giờ hỏng mà game gặp: thiếu số 0, rỗng, chỉ khoảng trắng, chữ, tháng 13, ngày kiểu Mỹ, cách
            // thay cho T — kèm neo luật hỏng. Hub phải thấy lại ĐÚNG chuỗi đã gõ sau khi xuất và đọc lại, không phải một giờ đã đoán.
            string[] brokenTexts = { "2026-10-3", string.Empty, "   ", "not a time", "2026-13-01T00:00:00Z", "3/10/2026", "2026-09-10 08:00" };
            var builder = new LiveEventCalendarDocumentBuilder(LiveOpsDesignSample.Document)
                .WithRecurringRule(new RecurringLiveEventRule("broken-anchor", "2026-1-5", string.Empty, 24, 20, string.Empty));
            for (int index = 0; index < brokenTexts.Length; index++)
            {
                string suffix = index.ToString(CultureInfo.InvariantCulture);
                builder.WithFixedEvent(new FixedLiveEventEntry("broken-start-" + suffix, "broken-start-" + suffix, "lava-quest", brokenTexts[index],
                    "2026-11-02T00:00:00Z", string.Empty));
                builder.WithFixedEvent(new FixedLiveEventEntry("broken-end-" + suffix, "broken-end-" + suffix, "lava-quest", "2026-11-01T00:00:00Z",
                    brokenTexts[index], "quote\"back\\slash"));
            }
            LiveEventCalendarDocument document = builder.Build();

            for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
            {
                LiveEventCalendarJsonFormat format = Formats[formatIndex];
                LiveEventCalendarDocument parsed = ParseWritten("Chuỗi hỏng", document, format);

                for (int index = 0; index < brokenTexts.Length; index++)
                {
                    string suffix = index.ToString(CultureInfo.InvariantCulture);
                    Assert.AreEqual(brokenTexts[index], FindByEventId(parsed, "broken-start-" + suffix).StartUtcText,
                        "startUtc hỏng phải đọc lại nguyên văn: '" + brokenTexts[index] + "'.");
                    Assert.AreEqual(brokenTexts[index], FindByEventId(parsed, "broken-end-" + suffix).EndUtcText,
                        "endUtc hỏng phải đọc lại nguyên văn: '" + brokenTexts[index] + "'.");
                }
                Assert.AreEqual("2026-10-3", FindByEventId(parsed, "lava-quest-2026-10").EndUtcText);
                if (format == LiveEventCalendarJsonFormat.Version2)
                {
                    Assert.AreEqual("2026-1-5", parsed.RecurringRules[2].AnchorUtcText, "Neo luật hỏng cũng giữ nguyên văn.");
                }

                AssertByteFixedPoint("Chuỗi hỏng", document, format);
                AssertJsonEquivalentAfterParse("Chuỗi hỏng", document, format);
                AssertCompiledEquivalentAfterParse("Chuỗi hỏng", document, format);
            }
        }

        [Test]
        public void RecurringAbsentNullOrEmpty_ReadsNoRules_WhitespaceJsonBlockedBeforeFromJson()
        {
            // SP-11 (cổng W1): mảng vắng → null, "recurring": null / [] → mảng rỗng; JsonUtility NÉM với chuỗi chỉ khoảng trắng nên
            // parser phải chặn trước FromJson. Khứ hồi định dạng 1 dựa vào đúng hai điều này.
            string[] noRuleJsons =
            {
                "{\"events\":[]}",
                "{\"version\":2,\"recurring\":null,\"events\":[]}",
                "{\"version\":2,\"recurring\":[],\"events\":[]}",
                LiveEventCalendarJsonWriter.Write(LiveOpsDesignSample.Document, LiveEventCalendarJsonFormat.Version1).Text,
                LiveEventCalendarJsonWriter.Write(LiveEventCalendarDocument.Empty, LiveEventCalendarJsonFormat.Version2).Text,
            };
            int[] expectedFormatVersions = { 1, 2, 2, 1, 2 };
            for (int index = 0; index < noRuleJsons.Length; index++)
            {
                LiveEventCalendarDocumentParseResult read = JsonLiveEventCalendarParser.ParseDocument(noRuleJsons[index]);
                Assert.IsTrue(read.IsReadable, noRuleJsons[index]);
                Assert.AreEqual(expectedFormatVersions[index], read.FormatVersion, noRuleJsons[index]);
                Assert.AreEqual(0, read.Document.RecurringRules.Count, noRuleJsons[index]);
                CollectionAssert.IsEmpty(read.Problems, noRuleJsons[index]);
            }

            string[] blankJsons = { "   ", " \n\t\r\n " };
            for (int index = 0; index < blankJsons.Length; index++)
            {
                LiveEventCalendarDocumentParseResult read = null;
                Assert.DoesNotThrow(() => read = JsonLiveEventCalendarParser.ParseDocument(blankJsons[index]));
                Assert.IsTrue(read.IsReadable, "Chuỗi trắng là 'remote chưa có key', không phải JSON hỏng.");
                Assert.IsTrue(read.IsBlank);
                Assert.AreEqual(0, read.FormatVersion);
                CollectionAssert.IsEmpty(read.Problems, "Chuỗi trắng không được lọt tới FromJson (ArgumentException → Problem 'JSON hỏng').");
                Assert.AreSame(FixedLiveEventCalendar.Empty, JsonLiveEventCalendarParser.Parse(blankJsons[index]).Calendar);
            }
        }

        // ----- Parser game ↔ validator ↔ asset -----

        [Test]
        public void DesignSample_ParserKeeps6Of8_MatchesValidatorDroppedCount()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            string json = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2).Text;
            Assert.AreEqual(LiveOpsDesignSample.ExpectedFormat2Json, json, "Tiền đề: bộ ghi ra đúng 1.601 byte mẫu.");

            LiveEventCalendarParseResult parsed = JsonLiveEventCalendarParser.Parse(json);
            Assert.AreEqual(2, parsed.FormatVersion);
            Assert.AreEqual(8, parsed.Compilation.EntryCount, "2 luật lặp + 6 đợt cố định.");
            Assert.AreEqual(6, parsed.Compilation.KeptCount);

            LiveEventCalendarCheckReport report = CheckDocument(document);
            Assert.AreEqual(2, CountRulesOneToSevenFindings(report), "utc-time-format · lava-quest-2026-10 và overlap-same-type · hunt-0916-bonus.");
            Assert.AreEqual(parsed.Compilation.DroppedCount, CountRulesOneToSevenFindings(report),
                "Kiểm lịch nói bỏ bao nhiêu mục thì game đọc JSON xuất bỏ đúng bấy nhiêu.");
            Assert.AreEqual(parsed.Compilation.DroppedCount, report.Summary.DroppedCount, "Mẫu khai đủ loại nên luật 8 không thêm Bị bỏ.");
        }

        [Test]
        public void Format1_ParserKeeps4Of6()
        {
            LiveEventCalendarDocument document = LiveOpsDesignSample.Document;
            string json = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version1).Text;

            LiveEventCalendarParseResult parsed = JsonLiveEventCalendarParser.Parse(json);

            Assert.AreEqual(1, parsed.FormatVersion);
            Assert.AreEqual(0, parsed.RecurringCalendars.Count, "Định dạng 1 không mang luật lặp — game 0.1.0 chỉ thấy đợt cố định.");
            Assert.AreEqual(6, parsed.Compilation.EntryCount);
            Assert.AreEqual(4, parsed.Compilation.KeptCount, "Bỏ lava-quest-2026-10 (endUtc hỏng) và hunt-0916-bonus (chồng giờ) — [SD2 §3.10] 'giữ 4/6 mục'.");
            Assert.AreEqual(4, parsed.Calendar.Instances.Count);

            LiveEventCalendarCheckReport report = CheckDocument(WithoutRecurringRules(document));
            Assert.AreEqual(parsed.Compilation.DroppedCount, CountRulesOneToSevenFindings(report));
        }

        [Test]
        public void AssetToParseResult_EqualsJsonParse()
        {
            LiveEventCalendarAsset asset = CreateAsset();
            AssertAssetParseResultEqualsJsonParse("Mẫu thiết kế", asset, LiveOpsDesignSample.Document);
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                AssertAssetParseResultEqualsJsonParse(LiveOpsRandomDocuments.Label(documentIndex), asset, LiveOpsRandomDocuments.Create(documentIndex));
            }
        }

        [Test]
        public void DuplicateIdLaterStartFirstInAsset_ValidatorAssetAndJsonKeepSameEntry()
        {
            // Hai đợt cùng id "hunt-dup", đợt bắt đầu MUỘN đứng trước trong asset. Thứ tự asset sẽ giữ "late-first"; game đọc JSON
            // đã sắp giữ "early-second". Kiểm lịch, game đọc asset và game đọc JSON xuất phải cùng giữ "early-second" (V-6).
            LiveEventCalendarDocument document = new LiveEventCalendarDocumentBuilder()
                .WithEventType(new LiveEventTypeDefinition("hunt", "Săn", 7, false, "hunt_default"))
                .WithFixedEvent(new FixedLiveEventEntry("late-first", "hunt-dup", "hunt", "2026-10-05T00:00:00Z", "2026-10-06T00:00:00Z", "hunt_late"))
                .WithFixedEvent(new FixedLiveEventEntry("unrelated", "hunt-other", "hunt", "2026-10-10T00:00:00Z", "2026-10-11T00:00:00Z", string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry("early-second", "hunt-dup", "hunt", "2026-10-01T00:00:00Z", "2026-10-02T00:00:00Z", "hunt_early"))
                .Build();
            var expectedStartUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

            LiveEventCalendarCheckReport report = CheckDocument(document);
            List<LiveEventCalendarFinding> duplicateFindings = FindingsOf(report, LiveEventCalendarRuleIds.DuplicateEventId);
            Assert.AreEqual(1, duplicateFindings.Count, "Đúng một đợt bị bỏ vì trùng id.");
            Assert.AreEqual("late-first", duplicateFindings[0].TargetEntryKey, "Kiểm lịch phải báo bỏ đợt bắt đầu muộn — đúng như game đọc JSON xuất.");

            LiveEventCalendarAsset asset = CreateAsset();
            asset.ApplyDocument(document);
            LiveEventCalendarParseResult assetResult = asset.ToParseResult();
            AssertKeepsOnly("Game đọc asset", assetResult, "hunt-dup", expectedStartUtc, "hunt_early");

            for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
            {
                LiveEventCalendarJsonFormat format = Formats[formatIndex];
                string json = LiveEventCalendarJsonWriter.Write(asset.ToDocument(), format).Text;
                LiveEventCalendarParseResult jsonResult = JsonLiveEventCalendarParser.Parse(json);
                AssertKeepsOnly("Game đọc JSON định dạng " + ((int)format).ToString(CultureInfo.InvariantCulture), jsonResult, "hunt-dup",
                    expectedStartUtc, "hunt_early");
                LiveOpsDocumentAssert.AssertCompiledOutcomesEqual("Asset ↔ JSON định dạng " + ((int)format).ToString(CultureInfo.InvariantCulture),
                    assetResult.Compilation.Entries, jsonResult.Compilation.Entries);
            }

            Assert.IsTrue(LiveEventCalendarCompiler.Compile(document).TryGetFixedOutcome("late-first", out LiveEventCalendarEntryOutcome assetOrderOutcome));
            Assert.IsTrue(assetOrderOutcome.IsKept, "Đối chứng: biên dịch theo thứ tự asset giữ đợt khác — test đang khoá đúng thứ tự xuất.");
        }

        // ----- Fuzz 200 tài liệu -----

        [Test]
        public void RandomDocuments_RoundTrip()
        {
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                string label = LiveOpsRandomDocuments.Label(documentIndex);
                for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
                {
                    LiveEventCalendarJsonFormat format = Formats[formatIndex];
                    AssertByteFixedPoint(label, document, format);
                    AssertJsonEquivalentAfterParse(label, document, format);
                    AssertCompiledEquivalentAfterParse(label, document, format);
                }
            }
        }

        [Test]
        public void RandomDocuments_CompiledKeptSetEqual_AssetVersusWrittenJson()
        {
            LiveEventCalendarAsset asset = CreateAsset();
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                string label = LiveOpsRandomDocuments.Label(documentIndex);
                asset.ApplyDocument(document);
                LiveEventCalendarDocument assetDocument = asset.ToDocument();

                LiveEventCalendarParseResult assetResult = asset.ToParseResult();
                LiveEventCalendarParseResult jsonResult = JsonLiveEventCalendarParser.Parse(
                    LiveEventCalendarJsonWriter.Write(assetDocument, LiveEventCalendarJsonFormat.Version2).Text);

                // V-6 (c): dãy (EventId, IsKept, DropReason) theo thứ tự — cùng tập mục được giữ, kể cả khi trùng id.
                AssertSupport(label, document, () =>
                    LiveOpsDocumentAssert.AssertCompiledOutcomesEqual(label + " · Entries", assetResult.Compilation.Entries, jsonResult.Compilation.Entries));
                CollectionAssert.AreEqual(KeptFixedEventIds(assetResult.Compilation), KeptFixedEventIds(jsonResult.Compilation),
                    label + ": tập đợt được giữ khác nhau giữa asset và JSON xuất.\n" + LiveOpsRandomDocuments.Describe(document));

                // V-6 (d): Kiểm lịch trên asset và parser game trên JSON xuất cùng số Bị bỏ luật 1–7.
                Assert.AreEqual(jsonResult.Compilation.DroppedCount, CountRulesOneToSevenFindings(CheckDocument(assetDocument)),
                    label + ": Kiểm lịch báo số mục bị bỏ khác số mục game đọc JSON xuất bỏ.\n" + LiveOpsRandomDocuments.Describe(document));
            }
        }

        // ----- Chính hàm so tương đương (không để hàm so luôn-xanh che lỗi bộ ghi) -----

        [Test]
        public void AreJsonEquivalent_RejectsTickOrderConfigKeyAndVerbatimDifferences()
        {
            LiveEventCalendarDocument expected = HuntDocument(
                new FixedLiveEventEntry("first", "hunt-a", "hunt", "2026-09-10T00:00:00.2Z", "2026-09-11T00:00:00Z", string.Empty),
                new FixedLiveEventEntry("second", "hunt-b", "hunt", "2026-09-12T00:00:00Z", "2026-10-3", "hunt_b"));

            // Cùng thời điểm viết khác (múi +07:00), configKey hiệu lực ghi tường minh, không khai loại: vẫn tương đương.
            LiveEventCalendarDocument sameMeaning = new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("json-0", "hunt-a", "hunt", "2026-09-10T07:00:00.2+07:00", "2026-09-11T00:00:00Z", "hunt_default"))
                .WithFixedEvent(new FixedLiveEventEntry("json-1", "hunt-b", "hunt", "2026-09-12T00:00:00Z", "2026-10-3", "hunt_b"))
                .Build();
            Assert.DoesNotThrow(() => LiveOpsDocumentAssert.AreJsonEquivalent(expected, sameMeaning, LiveEventCalendarJsonFormat.Version2));

            AssertNotEquivalent("phần lẻ giây .2 → .7 (Format cắt về giây sẽ coi là bằng)", expected, new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("json-0", "hunt-a", "hunt", "2026-09-10T00:00:00.7Z", "2026-09-11T00:00:00Z", "hunt_default"))
                .WithFixedEvent(new FixedLiveEventEntry("json-1", "hunt-b", "hunt", "2026-09-12T00:00:00Z", "2026-10-3", "hunt_b"))
                .Build(), "tick");
            AssertNotEquivalent("thứ tự events khác thứ tự xuất", expected, new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("json-0", "hunt-b", "hunt", "2026-09-12T00:00:00Z", "2026-10-3", "hunt_b"))
                .WithFixedEvent(new FixedLiveEventEntry("json-1", "hunt-a", "hunt", "2026-09-10T00:00:00.2Z", "2026-09-11T00:00:00Z", "hunt_default"))
                .Build(), "mục thứ 1");
            AssertNotEquivalent("configKey hiệu lực khác", expected, new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("json-0", "hunt-a", "hunt", "2026-09-10T00:00:00.2Z", "2026-09-11T00:00:00Z", "hunt_other"))
                .WithFixedEvent(new FixedLiveEventEntry("json-1", "hunt-b", "hunt", "2026-09-12T00:00:00Z", "2026-10-3", "hunt_b"))
                .Build(), "configKey");
            AssertNotEquivalent("giờ hỏng khác nguyên văn", expected, new LiveEventCalendarDocumentBuilder()
                .WithFixedEvent(new FixedLiveEventEntry("json-0", "hunt-a", "hunt", "2026-09-10T00:00:00.2Z", "2026-09-11T00:00:00Z", "hunt_default"))
                .WithFixedEvent(new FixedLiveEventEntry("json-1", "hunt-b", "hunt", "2026-09-12T00:00:00Z", "2026-10-03", "hunt_b"))
                .Build(), "nguyên văn");

            // Định dạng 1: luật của bản gốc không được so, nhưng bản đọc lại có luật là sai (mảng recurring lọt vào JSON 1).
            LiveEventCalendarDocument withRule = new LiveEventCalendarDocumentBuilder(expected)
                .WithRecurringRule(new RecurringLiveEventRule("hunt", "2026-01-05T00:00:00Z", string.Empty, 24, 20, string.Empty))
                .Build();
            Assert.DoesNotThrow(() => LiveOpsDocumentAssert.AreJsonEquivalent(withRule, sameMeaning, LiveEventCalendarJsonFormat.Version1));
            InvalidOperationException formatOneException = Assert.Throws<InvalidOperationException>(
                () => LiveOpsDocumentAssert.AreJsonEquivalent(expected, withRule, LiveEventCalendarJsonFormat.Version1));
            StringAssert.Contains("recurring", formatOneException.Message);
        }

        // ----- Phép so -----

        /// <summary>Phép so 1 (chính): <c>Write(ParseDocument(Write(d)).Document) == Write(d)</c> từng byte — đúng thứ game đọc.</summary>
        private static void AssertByteFixedPoint(string label, LiveEventCalendarDocument document, LiveEventCalendarJsonFormat format)
        {
            string formatLabel = label + " · định dạng " + ((int)format).ToString(CultureInfo.InvariantCulture);
            LiveEventCalendarJsonText first = LiveEventCalendarJsonWriter.Write(document, format);
            LiveEventCalendarDocument parsed = ParseWritten(formatLabel, document, format);
            LiveEventCalendarJsonText second = LiveEventCalendarJsonWriter.Write(parsed, format);

            byte[] firstBytes = first.GetUtf8Bytes();
            byte[] secondBytes = second.GetUtf8Bytes();
            int firstDifference = FirstDifferenceIndex(firstBytes, secondBytes);
            if (firstDifference >= 0)
            {
                Assert.Fail(formatLabel + ": ghi → đọc → ghi không ra đúng byte cũ (lệch từ byte " + firstDifference.ToString(CultureInfo.InvariantCulture) +
                            ", " + firstBytes.Length.ToString(CultureInfo.InvariantCulture) + " → " + secondBytes.Length.ToString(CultureInfo.InvariantCulture) +
                            " byte).\n--- lần 1 ---\n" + first.Text + "\n--- lần 2 ---\n" + second.Text + "\n" + LiveOpsRandomDocuments.Describe(document));
            }
            Assert.AreEqual(first.Sha256Hex, second.Sha256Hex, formatLabel + ": cùng byte phải cùng sha (dấu đã đăng so bằng sha).");
            Assert.IsTrue(LiveEventCalendarExportOrder.IsInExportOrder(parsed), formatLabel + ": JSON xuất đọc lại phải đã ở thứ tự xuất (V-6 b).");
        }

        /// <summary>Phép so 2: tài liệu đọc lại tương đương theo JSON với tài liệu gốc (<see cref="LiveOpsDocumentAssert.AreJsonEquivalent"/>).</summary>
        private static void AssertJsonEquivalentAfterParse(string label, LiveEventCalendarDocument document, LiveEventCalendarJsonFormat format)
        {
            string formatLabel = label + " · định dạng " + ((int)format).ToString(CultureInfo.InvariantCulture);
            LiveEventCalendarDocument parsed = ParseWritten(formatLabel, document, format);
            AssertSupport(formatLabel, document, () => LiveOpsDocumentAssert.AreJsonEquivalent(document, parsed, format));
        }

        /// <summary>
        /// Phép so 3: <c>CompileInExportOrder(d)</c> và <c>Compile(parsed)</c> cùng dãy mục và cùng lần chạy trong một năm. Định dạng 1
        /// không mang luật lặp, nên bản gốc được biên dịch KHÔNG có luật — đúng cái game 0.1.0 thấy từ JSON đó.
        /// </summary>
        private static void AssertCompiledEquivalentAfterParse(string label, LiveEventCalendarDocument document, LiveEventCalendarJsonFormat format)
        {
            string formatLabel = label + " · định dạng " + ((int)format).ToString(CultureInfo.InvariantCulture);
            LiveEventCalendarDocument parsed = ParseWritten(formatLabel, document, format);
            LiveEventCalendarDocument source = format == LiveEventCalendarJsonFormat.Version2 ? document : WithoutRecurringRules(document);
            AssertCompiledEquivalent(formatLabel, document, LiveEventCalendarCompiler.CompileInExportOrder(source), LiveEventCalendarCompiler.Compile(parsed));
        }

        private static LiveEventCalendarDocument ParseWritten(string label, LiveEventCalendarDocument document, LiveEventCalendarJsonFormat format)
        {
            string json = LiveEventCalendarJsonWriter.Write(document, format).Text;
            LiveEventCalendarDocumentParseResult read = JsonLiveEventCalendarParser.ParseDocument(json);
            Assert.IsTrue(read.IsReadable, label + ": JsonUtility không đọc được JSON của bộ ghi — " + read.ReadErrorText + "\n" + json);
            Assert.AreEqual((int)format, read.FormatVersion, label + ": định dạng đọc lại khác định dạng đã ghi.\n" + json);
            CollectionAssert.IsEmpty(read.Problems, label + ": đọc JSON của bộ ghi không được sinh vấn đề cấp đọc.\n" + json);
            return read.Document;
        }

        private static void AssertCompiledEquivalent(string label, LiveEventCalendarDocument describedDocument, LiveEventCalendarCompilation expected,
            LiveEventCalendarCompilation actual)
        {
            Assert.AreEqual(expected.EntryCount, actual.EntryCount, label + ": EntryCount.\n" + LiveOpsRandomDocuments.Describe(describedDocument));
            AssertSupport(label, describedDocument, () => LiveOpsDocumentAssert.AssertCompiledOutcomesEqual(label + " · Entries", expected.Entries, actual.Entries));
            AssertInstanceListsEqual(label + " · FixedCalendar", describedDocument, expected.FixedCalendar.Instances, actual.FixedCalendar.Instances);

            List<string> eventTypes = EventTypesOf(describedDocument);
            for (int index = 0; index < eventTypes.Count; index++)
            {
                string eventType = eventTypes[index];
                AssertInstanceListsEqual(label + " · lần chạy '" + eventType + "' trong một năm", describedDocument,
                    expected.GetInstancesInRange(eventType, InstanceWindowStartUtc, InstanceWindowEndUtc),
                    actual.GetInstancesInRange(eventType, InstanceWindowStartUtc, InstanceWindowEndUtc));
            }
        }

        private static void AssertInstanceListsEqual(string label, LiveEventCalendarDocument describedDocument, IReadOnlyList<LiveEventInstance> expected,
            IReadOnlyList<LiveEventInstance> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count, label + ": số lần chạy khác nhau.\n" + LiveOpsRandomDocuments.Describe(describedDocument));
            for (int index = 0; index < expected.Count; index++)
            {
                LiveEventInstance expectedInstance = expected[index];
                LiveEventInstance actualInstance = actual[index];
                string itemLabel = label + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
                bool same = string.Equals(expectedInstance.EventId, actualInstance.EventId, StringComparison.Ordinal) &&
                            string.Equals(expectedInstance.EventType, actualInstance.EventType, StringComparison.Ordinal) &&
                            expectedInstance.StartUtc.Ticks == actualInstance.StartUtc.Ticks &&
                            expectedInstance.EndUtc.Ticks == actualInstance.EndUtc.Ticks &&
                            string.Equals(expectedInstance.ConfigKey, actualInstance.ConfigKey, StringComparison.Ordinal);
                if (!same)
                {
                    Assert.Fail(itemLabel + ": mong " + DescribeInstance(expectedInstance) + ", thấy " + DescribeInstance(actualInstance) + ".\n" +
                                LiveOpsRandomDocuments.Describe(describedDocument));
                }
            }
        }

        private void AssertAssetParseResultEqualsJsonParse(string label, LiveEventCalendarAsset asset, LiveEventCalendarDocument document)
        {
            asset.ApplyDocument(document);
            LiveEventCalendarDocument assetDocument = asset.ToDocument();
            LiveEventCalendarParseResult assetResult = asset.ToParseResult();
            string json = LiveEventCalendarJsonWriter.Write(assetDocument, LiveEventCalendarJsonFormat.Version2).Text;
            LiveEventCalendarParseResult jsonResult = JsonLiveEventCalendarParser.Parse(json);

            Assert.IsTrue(assetResult.CameFromDefaultCalendar, label);
            Assert.IsFalse(jsonResult.CameFromDefaultCalendar, label);
            Assert.AreEqual(2, assetResult.FormatVersion, label);
            Assert.AreEqual(assetResult.FormatVersion, jsonResult.FormatVersion, label);
            CollectionAssert.AreEqual(assetResult.Problems, jsonResult.Problems,
                label + ": câu Problems của game đọc asset khác game đọc JSON xuất (\"mục thứ N\" phải theo thứ tự xuất).\n" + json);
            Assert.AreEqual(assetResult.RecurringCalendars.Count, jsonResult.RecurringCalendars.Count, label);
            AssertCompiledEquivalent(label, document, assetResult.Compilation, jsonResult.Compilation);
        }

        private static void AssertKeepsOnly(string label, LiveEventCalendarParseResult result, string eventId, DateTime expectedStartUtc, string expectedConfigKey)
        {
            int keptCount = 0;
            IReadOnlyList<LiveEventInstance> instances = result.Calendar.Instances;
            for (int index = 0; index < instances.Count; index++)
            {
                if (!string.Equals(instances[index].EventId, eventId, StringComparison.Ordinal)) continue;
                keptCount++;
                Assert.AreEqual(expectedStartUtc, instances[index].StartUtc, label + ": giữ nhầm đợt trùng id.");
                Assert.AreEqual(expectedConfigKey, instances[index].ConfigKey, label);
            }
            Assert.AreEqual(1, keptCount, label + ": phải giữ đúng một đợt '" + eventId + "'.");
        }

        // ----- Tiện ích -----

        /// <summary>
        /// Hàm so của assembly hỗ trợ ném <see cref="InvalidOperationException"/> (không có NUnit ở đó) — đổi thành Assert.Fail có
        /// nhãn tài liệu + dump field thô, để một fuzz đỏ tự nói tài liệu nào và trông ra sao.
        /// </summary>
        private static void AssertSupport(string label, LiveEventCalendarDocument document, Action comparison)
        {
            try
            {
                comparison();
            }
            catch (InvalidOperationException exception)
            {
                Assert.Fail(label + ": " + exception.Message + "\n" + LiveOpsRandomDocuments.Describe(document));
            }
        }

        private static void AssertNotEquivalent(string caseLabel, LiveEventCalendarDocument expected, LiveEventCalendarDocument actual, string messageFragment)
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => LiveOpsDocumentAssert.AreJsonEquivalent(expected, actual, LiveEventCalendarJsonFormat.Version2), caseLabel);
            StringAssert.Contains(messageFragment, exception.Message, caseLabel);
        }

        private static LiveEventCalendarDocument HuntDocument(params FixedLiveEventEntry[] entries)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithEventType(new LiveEventTypeDefinition("hunt", "Săn", 7, false, "hunt_default"));
            for (int index = 0; index < entries.Length; index++) builder.WithFixedEvent(entries[index]);
            return builder.Build();
        }

        private static LiveEventCalendarDocument WithoutRecurringRules(LiveEventCalendarDocument document)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            for (int index = 0; index < document.EventTypes.Count; index++) builder.WithEventType(document.EventTypes[index]);
            for (int index = 0; index < document.FixedEvents.Count; index++) builder.WithFixedEvent(document.FixedEvents[index]);
            for (int index = 0; index < document.PublishedStamps.Count; index++) builder.WithPublishedStamp(document.PublishedStamps[index]);
            for (int index = 0; index < document.IgnoredWarnings.Count; index++) builder.WithIgnoredWarning(document.IgnoredWarnings[index]);
            return builder.Build();
        }

        private static List<string> EventTypesOf(LiveEventCalendarDocument document)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var eventTypes = new List<string>();
            for (int index = 0; index < document.EventTypes.Count; index++) AddOnce(seen, eventTypes, document.EventTypes[index].TypeId);
            for (int index = 0; index < document.RecurringRules.Count; index++) AddOnce(seen, eventTypes, document.RecurringRules[index].EventType);
            for (int index = 0; index < document.FixedEvents.Count; index++) AddOnce(seen, eventTypes, document.FixedEvents[index].EventType);
            eventTypes.Sort(StringComparer.Ordinal);
            return eventTypes;
        }

        private static void AddOnce(HashSet<string> seen, List<string> values, string value)
        {
            if (seen.Add(value)) values.Add(value);
        }

        private static List<string> KeptFixedEventIds(LiveEventCalendarCompilation compilation)
        {
            var keptIds = new List<string>();
            for (int index = 0; index < compilation.Entries.Count; index++)
            {
                LiveEventCalendarEntryOutcome outcome = compilation.Entries[index];
                if (outcome.Kind == LiveEventCalendarEntryKind.FixedEvent && outcome.IsKept) keptIds.Add(outcome.EventId);
            }
            keptIds.Sort(StringComparer.Ordinal);
            return keptIds;
        }

        private static LiveEventCalendarCheckReport CheckDocument(LiveEventCalendarDocument document)
        {
            return LiveEventCalendarValidator.Default.Check(new LiveEventCalendarCheckContextBuilder(document, LiveOpsDesignSample.NowUtc).Build());
        }

        private static int CountRulesOneToSevenFindings(LiveEventCalendarCheckReport report)
        {
            int count = 0;
            for (int index = 0; index < report.RuleResults.Count; index++)
            {
                LiveEventCalendarRuleResult result = report.RuleResults[index];
                if (RulesOneToSeven.Contains(result.RuleId)) count += result.Findings.Count;
            }
            return count;
        }

        private static List<LiveEventCalendarFinding> FindingsOf(LiveEventCalendarCheckReport report, string ruleId)
        {
            var findings = new List<LiveEventCalendarFinding>();
            for (int index = 0; index < report.RuleResults.Count; index++)
            {
                LiveEventCalendarRuleResult result = report.RuleResults[index];
                if (string.Equals(result.RuleId, ruleId, StringComparison.Ordinal)) findings.AddRange(result.Findings);
            }
            return findings;
        }

        private static FixedLiveEventEntry FindByEventId(LiveEventCalendarDocument document, string eventId)
        {
            for (int index = 0; index < document.FixedEvents.Count; index++)
            {
                if (string.Equals(document.FixedEvents[index].EventId, eventId, StringComparison.Ordinal)) return document.FixedEvents[index];
            }
            Assert.Fail("Không thấy đợt '" + eventId + "' trong tài liệu đọc lại.");
            return null;
        }

        private static int FirstDifferenceIndex(byte[] first, byte[] second)
        {
            int sharedLength = Math.Min(first.Length, second.Length);
            for (int index = 0; index < sharedLength; index++)
            {
                if (first[index] != second[index]) return index;
            }
            return first.Length == second.Length ? -1 : sharedLength;
        }

        private static string DescribeInstance(LiveEventInstance instance)
        {
            return "'" + instance.EventId + "' " + instance.EventType + " " + instance.StartUtc.Ticks.ToString(CultureInfo.InvariantCulture) + "→" +
                   instance.EndUtc.Ticks.ToString(CultureInfo.InvariantCulture) + " '" + instance.ConfigKey + "'";
        }
    }
}
