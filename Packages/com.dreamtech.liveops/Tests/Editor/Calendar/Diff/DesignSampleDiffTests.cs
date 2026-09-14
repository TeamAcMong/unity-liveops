using System;
using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// Diff trên dữ liệu mẫu của thiết kế (13/9/2026 08:47 UTC, bản so = lần đăng 11/9 16:20) — khoá đúng các hàng card diff
    /// [SD2 §3.8]. Bản so dựng bằng cách bỏ EventTypes khỏi tài liệu mẫu đã đăng, mô phỏng đúng
    /// <c>ParseDocument(SnapshotJson)</c> mà không phụ thuộc parser của G-UNITY (V-3): JSON không mang định nghĩa loại.
    /// </summary>
    [TestFixture]
    public sealed class DesignSampleDiffTests
    {
        private static readonly DateTime NowUtc = LiveOpsDesignSample.NowUtc;

        [Test]
        public void DesignSample_Diff_MatchesDesignRows()
        {
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(PublishedBaselineDocument(), LiveOpsDesignSample.Document, NowUtc);

            // Chip đầu card: "Thêm 1" · "Đổi 4" · "Xoá 0" · "Giữ 3"; 8 mục = 6 đợt + 2 luật, KHÔNG tính 5 loại.
            Assert.AreEqual(1, result.AddedCount);
            Assert.AreEqual(4, result.ChangedCount);
            Assert.AreEqual(0, result.RemovedCount);
            Assert.AreEqual(3, result.KeptCount);
            Assert.AreEqual(5, result.ChangeCount, "\"5 thay đổi\"");
            Assert.AreEqual(1, result.ReviewRequiredCount, "\"bắt buộc 0/1\"");
            Assert.AreEqual(5, result.Changes.Count);

            // Nhóm BỊ BỎ KHI GAME ĐỌC LỊCH (2): hunt-0916-bonus (thêm mới) rồi lava-quest-2026-10 endUtc — theo giờ bắt đầu.
            LiveEventCalendarChange huntBonus = result.Changes[0];
            Assert.AreEqual("hunt-0916-bonus", huntBonus.ItemId);
            Assert.AreEqual(LiveEventCalendarChangeKind.Added, huntBonus.Kind);
            Assert.AreEqual(LiveEventCalendarItemKind.FixedEvent, huntBonus.ItemKind);
            Assert.AreEqual(LiveOpsDesignSample.HuntBonusEntryKey, huntBonus.EntryKey);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, huntBonus.Consequence);
            Assert.IsTrue(huntBonus.WillBeDropped);
            Assert.AreEqual(0, huntBonus.Fields.Count);

            LiveEventCalendarChange lateLava = result.Changes[1];
            Assert.AreEqual("lava-quest-2026-10", lateLava.ItemId);
            Assert.AreEqual(LiveEventCalendarChangeKind.Changed, lateLava.Kind);
            Assert.AreEqual(LiveEventCalendarConsequence.Dropped, lateLava.Consequence);
            Assert.IsTrue(lateLava.WillBeDropped);
            AssertSingleField(lateLava, "endUtc", "2026-10-03T00:00:00Z", "2026-10-3");

            // Nhóm NGƯỜI CHƠI MẤT TIẾN ĐỘ (1): weekly-pass tiền tố weekly-pass- → pass-, tag "bắt buộc".
            LiveEventCalendarChange weeklyPass = result.Changes[2];
            Assert.AreEqual("weekly-pass", weeklyPass.ItemId);
            Assert.AreEqual(LiveEventCalendarItemKind.RecurringRule, weeklyPass.ItemKind);
            Assert.AreEqual(LiveEventCalendarChangeKind.Changed, weeklyPass.Kind);
            Assert.AreEqual(LiveEventCalendarConsequence.ProgressLost, weeklyPass.Consequence);
            Assert.IsTrue(weeklyPass.IsReviewRequired);
            Assert.IsFalse(weeklyPass.WillBeDropped);
            AssertSingleField(weeklyPass, "idPrefix", "weekly-pass-", "pass-");
            Assert.AreEqual("weekly-pass-35", weeklyPass.RunningEventIdBefore);
            Assert.AreEqual("pass-35", weeklyPass.RunningEventIdAfter);
            Assert.AreEqual(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), weeklyPass.RunningEventEndUtc, "\"đang chạy tới 14/9 00:00 UTC\"");

            // Nhóm NÊN XEM (1): hunt-0914 configKey hunt_v1 → hunt_default (đợt không tự khai, xuất ghi mặc định của loại).
            LiveEventCalendarChange huntEarly = result.Changes[3];
            Assert.AreEqual("hunt-0914", huntEarly.ItemId);
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, huntEarly.Consequence);
            Assert.IsFalse(huntEarly.IsReviewRequired);
            AssertSingleField(huntEarly, "configKey", "hunt_v1", "hunt_default");
            Assert.AreEqual(string.Empty, huntEarly.RunningEventIdBefore, "Đợt chưa bắt đầu.");

            // AN TOÀN: lava-quest-2026-09b endUtc 19/9 → 20/9.
            LiveEventCalendarChange midLava = result.Changes[4];
            Assert.AreEqual("lava-quest-2026-09b", midLava.ItemId);
            Assert.AreEqual(LiveEventCalendarConsequence.Safe, midLava.Consequence);
            AssertSingleField(midLava, "endUtc", "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z");

            // "Đã xem 0/3": chỉ hàng không bị bỏ mới đánh dấu được.
            int reviewableCount = 0;
            foreach (LiveEventCalendarChange change in result.Changes)
            {
                if (!change.WillBeDropped) reviewableCount++;
            }
            Assert.AreEqual(3, reviewableCount);
        }

        [Test]
        public void PublishedBaselineWithoutTypes_TypesNeverReportedAdded()
        {
            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(PublishedBaselineDocument(), LiveOpsDesignSample.Document, NowUtc);
            foreach (LiveEventCalendarChange change in result.Changes)
            {
                Assert.AreNotEqual(LiveEventCalendarItemKind.EventType, change.ItemKind, "Bản so không có loại không được sinh 5 hàng \"thêm loại\".");
            }

            // Bản so = chính nháp xuất ra (bỏ loại): không có gì khác, dù nháp có 5 loại còn bản so không có loại nào.
            LiveEventCalendarDiffResult selfResult = LiveEventCalendarDiff.Compare(WithoutEventTypes(LiveOpsDesignSample.Document),
                LiveOpsDesignSample.Document, NowUtc);
            Assert.IsTrue(selfResult.IsEmpty, string.Join(" | ", selfResult.Changes));
            Assert.AreEqual(8, selfResult.KeptCount, "6 đợt + 2 luật.");
        }

        [Test]
        public void TypeDefaultConfigKeyChanged_ReportedOnInheritingItems()
        {
            LiveEventCalendarDocument draft = LiveOpsDesignSample.Document;
            LiveEventCalendarDocument baseline = WithoutEventTypes(draft);
            Assert.IsTrue(draft.TryGetEventType("treasure-hunt", out LiveEventTypeDefinition treasureHunt));
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(draft, new SetEventTypeEdit(treasureHunt.WithDefaultConfigKey("hunt_v2")),
                out LiveEventCalendarDocument changedDraft));

            LiveEventCalendarDiffResult result = LiveEventCalendarDiff.Compare(baseline, changedDraft, NowUtc);

            // hunt-0914 kế thừa → đổi; hunt-0916-bonus tự khai hunt_bonus → không đổi; không có hàng loại riêng (V-3).
            Assert.AreEqual(1, result.ChangeCount, string.Join(" | ", result.Changes));
            LiveEventCalendarChange change = result.Changes[0];
            Assert.AreEqual(LiveEventCalendarItemKind.FixedEvent, change.ItemKind);
            Assert.AreEqual("hunt-0914", change.ItemId);
            AssertSingleField(change, "configKey", "hunt_default", "hunt_v2");
            Assert.AreEqual(LiveEventCalendarConsequence.ShouldReview, change.Consequence, "Đợt chưa bắt đầu đổi configKey.");

            LiveEventCalendarDiffResult unsaved = LiveEventCalendarDiff.CompareByEntryKey(draft, changedDraft, NowUtc);
            Assert.AreEqual(2, unsaved.ChangeCount, "Chưa lưu: hàng loại + hàng đợt kế thừa.");
            Assert.AreEqual(LiveEventCalendarConsequence.Safe, FindChange(unsaved, LiveEventCalendarItemKind.EventType).Consequence);

            // Dấu vân đổi theo khoá hiệu lực: Toggle "Đã xem" của hunt-0914 phải tự tắt dù đợt không tự khai khoá.
            string fingerprintBefore = FindFingerprint(LiveEventCalendarDiff.Compare(PublishedBaselineDocument(), draft, NowUtc), "hunt-0914");
            string fingerprintAfter = FindFingerprint(LiveEventCalendarDiff.Compare(PublishedBaselineDocument(), changedDraft, NowUtc), "hunt-0914");
            Assert.AreNotEqual(fingerprintBefore, fingerprintAfter);
        }

        [Test]
        public void UnsavedAfterOneDrag_Is1()
        {
            LiveEventCalendarDocument current = LiveOpsDesignSample.Document;
            Assert.IsTrue(current.TryGetFixedEvent(LiveOpsDesignSample.LavaQuestMidEntryKey, out FixedLiveEventEntry midLava));
            // Bản đã lưu còn kết thúc 19/9; một lần kéo mép phải lên 20/9 → "Main.asset có 1 thay đổi chưa lưu".
            Assert.IsTrue(LiveEventCalendarEdits.TryApply(current,
                new ReplaceFixedEventEdit(midLava.WithTimes(midLava.StartUtcText, "2026-09-19T00:00:00Z")), out LiveEventCalendarDocument saved));

            LiveEventCalendarDiffResult unsaved = LiveEventCalendarDiff.CompareByEntryKey(saved, current, NowUtc);

            Assert.AreEqual(1, unsaved.ChangeCount);
            Assert.IsFalse(unsaved.IsEmpty);
            LiveEventCalendarChange change = unsaved.Changes[0];
            Assert.AreEqual(LiveEventCalendarChangeKind.Changed, change.Kind);
            Assert.AreEqual(LiveOpsDesignSample.LavaQuestMidEntryKey, change.EntryKey);
            Assert.AreEqual("lava-quest-2026-09b", change.ItemId);
            AssertSingleField(change, "endUtc", "2026-09-19T00:00:00Z", "2026-09-20T00:00:00Z");
            Assert.AreEqual(LiveEventCalendarConsequence.Safe, change.Consequence);
            Assert.AreEqual(12, unsaved.KeptCount, "6 đợt + 2 luật + 5 loại − 1 mục đổi.");

            Assert.IsTrue(LiveEventCalendarDiff.CompareByEntryKey(current, current, NowUtc).IsEmpty);
        }

        // ----- Dựng bản so -----

        /// <summary>
        /// Tài liệu mẫu ĐÃ ĐĂNG (11/9 16:20) — đúng nội dung <see cref="LiveOpsDesignSample.PublishedSnapshotJson"/> — đã bỏ
        /// EventTypes: configKey và tiền tố ghi rõ như JSON, EntryKey mới như parser sinh (danh tính diff theo id, không theo khoá).
        /// </summary>
        private static LiveEventCalendarDocument PublishedBaselineDocument()
        {
            LiveEventCalendarDocument baseline = new LiveEventCalendarDocumentBuilder()
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", "weekly-pass-", 168, 168, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry("published-0", "lava-quest-2026-09a", "lava-quest", "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("published-1", "hunt-0914", "treasure-hunt", "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", "hunt_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("published-2", "lava-quest-2026-09b", "lava-quest", "2026-09-17T00:00:00Z", "2026-09-19T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("published-3", "lava-quest-2026-10", "lava-quest", "2026-10-01T00:00:00Z", "2026-10-03T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("published-4", "star-tournament-2026-10", "star-tournament", "2026-10-03T00:00:00Z", "2026-10-06T00:00:00Z", "star_tournament_v1"))
                .Build();

            // Chốt bản dựng tay vẫn khớp chuỗi đã đăng của fixture: MỖI mục phải hiện thành đúng khối JSON đủ mọi field, theo đúng
            // thứ tự, và số mục phải bằng số khối trong JSON — sửa fixture (thêm/bớt mục, đổi giờ, đổi loại) là test đỏ ở đây,
            // không âm thầm làm diff mẫu so với một bản so khác bản đã đăng.
            string json = LiveOpsDesignSample.PublishedSnapshotJson;
            int searchFrom = 0;
            foreach (RecurringLiveEventRule rule in baseline.RecurringRules)
            {
                searchFrom = AssertBlockAfter(json, searchFrom,
                    "    {\n" +
                    "      \"type\": \"" + rule.EventType + "\",\n" +
                    "      \"anchorUtc\": \"" + rule.AnchorUtcText + "\",\n" +
                    "      \"idPrefix\": \"" + rule.IdPrefix + "\",\n" +
                    "      \"periodHours\": " + rule.PeriodHours.ToString(CultureInfo.InvariantCulture) + ",\n" +
                    "      \"activeHours\": " + rule.ActiveHours.ToString(CultureInfo.InvariantCulture) + ",\n" +
                    "      \"configKey\": \"" + rule.ConfigKey + "\"\n" +
                    "    }");
            }
            foreach (FixedLiveEventEntry entry in baseline.FixedEvents)
            {
                searchFrom = AssertBlockAfter(json, searchFrom,
                    "    {\n" +
                    "      \"id\": \"" + entry.EventId + "\",\n" +
                    "      \"type\": \"" + entry.EventType + "\",\n" +
                    "      \"startUtc\": \"" + entry.StartUtcText + "\",\n" +
                    "      \"endUtc\": \"" + entry.EndUtcText + "\",\n" +
                    "      \"configKey\": \"" + entry.ConfigKey + "\"\n" +
                    "    }");
            }
            Assert.AreEqual(baseline.RecurringRules.Count, CountOccurrences(json, "\"anchorUtc\": "), "Số luật lặp của bản so = số luật trong JSON đã đăng.");
            Assert.AreEqual(baseline.FixedEvents.Count, CountOccurrences(json, "\"id\": "), "Số đợt cố định của bản so = số đợt trong JSON đã đăng.");
            return baseline;
        }

        private static int AssertBlockAfter(string json, int searchFrom, string block)
        {
            int position = json.IndexOf(block, searchFrom, StringComparison.Ordinal);
            Assert.GreaterOrEqual(position, 0, "Khối không có (hoặc sai thứ tự) trong PublishedSnapshotJson:\n" + block);
            return position + block.Length;
        }

        private static int CountOccurrences(string text, string value)
        {
            int count = 0;
            for (int position = text.IndexOf(value, StringComparison.Ordinal); position >= 0;
                 position = text.IndexOf(value, position + value.Length, StringComparison.Ordinal))
            {
                count++;
            }
            return count;
        }

        /// <summary>Mô phỏng "xuất JSON rồi đọc lại": bỏ loại, ghi configKey/tiền tố hiệu lực, EntryKey mới.</summary>
        private static LiveEventCalendarDocument WithoutEventTypes(LiveEventCalendarDocument document)
        {
            var builder = new LiveEventCalendarDocumentBuilder().WithRemoteConfigKey(document.RemoteConfigKey);
            foreach (RecurringLiveEventRule rule in document.RecurringRules)
            {
                builder.WithRecurringRule(new RecurringLiveEventRule(rule.EventType, rule.AnchorUtcText, rule.EffectiveIdPrefix, rule.PeriodHours,
                    rule.ActiveHours, document.EffectiveConfigKeyOf(rule)));
            }
            int position = 0;
            foreach (FixedLiveEventEntry entry in LiveEventCalendarExportOrder.Apply(document).FixedEvents)
            {
                builder.WithFixedEvent(new FixedLiveEventEntry("read-back-" + position, entry.EventId, entry.EventType, entry.StartUtcText,
                    entry.EndUtcText, document.EffectiveConfigKeyOf(entry)));
                position++;
            }
            return builder.Build();
        }

        private static LiveEventCalendarChange FindChange(LiveEventCalendarDiffResult result, LiveEventCalendarItemKind itemKind)
        {
            foreach (LiveEventCalendarChange change in result.Changes)
            {
                if (change.ItemKind == itemKind) return change;
            }
            Assert.Fail("Không có thay đổi " + itemKind + ": " + string.Join(" | ", result.Changes));
            return null;
        }

        private static string FindFingerprint(LiveEventCalendarDiffResult result, string itemId)
        {
            var itemIds = new List<string>();
            foreach (LiveEventCalendarChange change in result.Changes)
            {
                if (change.ItemId == itemId) return change.Fingerprint;
                itemIds.Add(change.ItemId);
            }
            Assert.Fail("Không có thay đổi cho '" + itemId + "': " + string.Join(", ", itemIds));
            return null;
        }

        private static void AssertSingleField(LiveEventCalendarChange change, string fieldName, string beforeText, string afterText)
        {
            Assert.AreEqual(1, change.Fields.Count, "Field: " + string.Join(" | ", change.Fields));
            Assert.AreEqual(fieldName, change.Fields[0].FieldName);
            Assert.AreEqual(beforeText, change.Fields[0].BeforeText);
            Assert.AreEqual(afterText, change.Fields[0].AfterText);
        }
    }
}
