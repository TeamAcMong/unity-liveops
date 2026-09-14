using System;

namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// Tài liệu lịch mẫu dùng làm fixture cho mọi test của package — đúng dữ liệu ở mục "0.1 Dữ liệu mẫu" của kế
    /// hoạch P1: 5 loại, 2 luật, 6 đợt cố định, 1 dấu đã đăng. BÂY GIỜ = 13/9/2026 08:47 UTC; lần đăng gần nhất
    /// 11/9/2026 16:20 UTC (TRƯỚC năm thay đổi của nháp — xem <see cref="PublishedSnapshotJson"/>; các thay đổi đó chỉ
    /// nằm ở <see cref="Document"/>, không nằm ở dấu đã đăng bên trong nó).
    /// </summary>
    public static class LiveOpsDesignSample
    {
        public static readonly DateTime NowUtc = new DateTime(2026, 9, 13, 8, 47, 0, DateTimeKind.Utc);

        /// <summary>Giờ máy của Editor trong kịch bản mẫu — UTC+7.</summary>
        public static readonly TimeSpan DeviceOffset = TimeSpan.FromHours(7);

        public const string LavaQuestEarlyEntryKey = "entry-lava-quest-2026-09a";
        public const string HuntEarlyEntryKey = "entry-hunt-0914";
        public const string HuntBonusEntryKey = "entry-hunt-0916-bonus";
        public const string LavaQuestMidEntryKey = "entry-lava-quest-2026-09b";
        public const string LavaQuestLateEntryKey = "entry-lava-quest-2026-10";
        public const string StarTournamentEntryKey = "entry-star-tournament-2026-10";

        public const int PublishedByteCount = 1425;
        public const string PublishedSha256Hex = "5eecb84064b0b21a9d587c3d95e3a471f78c38595216415aba016f3e58702696";

        /// <summary>Người đăng + ghi chú của dấu 11/9 16:20 — đúng hàng mẫu "Các lần đã đăng" [SD2 §3.9].</summary>
        public const string PublishedPublisher = "DatHoUnityDev";
        public const string PublishedNote = "mở lava-quest tháng 9";

        /// <summary>
        /// Đúng nội dung bản đã đăng 11/9 16:20 (định dạng 2) — khác nháp đúng 5 chỗ của diff mẫu (mục 6.3, [SD2 §0]):
        /// tiền tố weekly-pass còn "weekly-pass-", hunt-0914 còn configKey "hunt_v1" (nháp kế thừa hunt_default),
        /// lava-quest-2026-09b còn kết thúc 19/9, lava-quest-2026-10 còn endUtc chuẩn "2026-10-03T00:00:00Z" (nháp gõ hỏng
        /// "2026-10-3"), chưa có hunt-0916-bonus. <see cref="PublishedByteCount"/>/<see cref="PublishedSha256Hex"/> tính trên
        /// đúng chuỗi này (UTF-8, không LF cuối) — test <c>DesignSample_PublishedStamp_MatchesSnapshotBytes</c> khoá lại.
        /// </summary>
        public static readonly string PublishedSnapshotJson =
            "{\n" +
            "  \"version\": 2,\n" +
            "  \"recurring\": [\n" +
            "    {\n" +
            "      \"type\": \"weekly-pass\",\n" +
            "      \"anchorUtc\": \"2026-01-05T00:00:00Z\",\n" +
            "      \"idPrefix\": \"weekly-pass-\",\n" +
            "      \"periodHours\": 168,\n" +
            "      \"activeHours\": 168,\n" +
            "      \"configKey\": \"weekly_pass_s3\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"type\": \"sky-race\",\n" +
            "      \"anchorUtc\": \"2026-01-05T00:00:00Z\",\n" +
            "      \"idPrefix\": \"sky-race-\",\n" +
            "      \"periodHours\": 24,\n" +
            "      \"activeHours\": 20,\n" +
            "      \"configKey\": \"sky_race_v4\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"events\": [\n" +
            "    {\n" +
            "      \"id\": \"lava-quest-2026-09a\",\n" +
            "      \"type\": \"lava-quest\",\n" +
            "      \"startUtc\": \"2026-09-10T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-13T00:00:00Z\",\n" +
            "      \"configKey\": \"lava_quest_v2\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"hunt-0914\",\n" +
            "      \"type\": \"treasure-hunt\",\n" +
            "      \"startUtc\": \"2026-09-14T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-17T00:00:00Z\",\n" +
            "      \"configKey\": \"hunt_v1\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"lava-quest-2026-09b\",\n" +
            "      \"type\": \"lava-quest\",\n" +
            "      \"startUtc\": \"2026-09-17T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-19T00:00:00Z\",\n" +
            "      \"configKey\": \"lava_quest_v2\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"lava-quest-2026-10\",\n" +
            "      \"type\": \"lava-quest\",\n" +
            "      \"startUtc\": \"2026-10-01T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-10-03T00:00:00Z\",\n" +
            "      \"configKey\": \"lava_quest_v2\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"star-tournament-2026-10\",\n" +
            "      \"type\": \"star-tournament\",\n" +
            "      \"startUtc\": \"2026-10-03T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-10-06T00:00:00Z\",\n" +
            "      \"configKey\": \"star_tournament_v1\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        /// <summary>
        /// Đúng byte định dạng 2 mà nháp hiện tại (<see cref="Document"/>) sẽ xuất ra — nguyên văn
        /// <c>plan/sample_format2.json</c> bỏ LF cuối (1.601 byte, 65 dòng). Dùng để khoá <c>LiveEventCalendarJsonWriter</c>
        /// (G-JSON) — G-CORE chỉ giữ hằng số này làm nguồn chuẩn, không tự ghi JSON.
        /// </summary>
        public static readonly string ExpectedFormat2Json =
            "{\n" +
            "  \"version\": 2,\n" +
            "  \"recurring\": [\n" +
            "    {\n" +
            "      \"type\": \"weekly-pass\",\n" +
            "      \"anchorUtc\": \"2026-01-05T00:00:00Z\",\n" +
            "      \"idPrefix\": \"pass-\",\n" +
            "      \"periodHours\": 168,\n" +
            "      \"activeHours\": 168,\n" +
            "      \"configKey\": \"weekly_pass_s3\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"type\": \"sky-race\",\n" +
            "      \"anchorUtc\": \"2026-01-05T00:00:00Z\",\n" +
            "      \"idPrefix\": \"sky-race-\",\n" +
            "      \"periodHours\": 24,\n" +
            "      \"activeHours\": 20,\n" +
            "      \"configKey\": \"sky_race_v4\"\n" +
            "    }\n" +
            "  ],\n" +
            "  \"events\": [\n" +
            "    {\n" +
            "      \"id\": \"lava-quest-2026-09a\",\n" +
            "      \"type\": \"lava-quest\",\n" +
            "      \"startUtc\": \"2026-09-10T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-13T00:00:00Z\",\n" +
            "      \"configKey\": \"lava_quest_v2\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"hunt-0914\",\n" +
            "      \"type\": \"treasure-hunt\",\n" +
            "      \"startUtc\": \"2026-09-14T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-17T00:00:00Z\",\n" +
            "      \"configKey\": \"hunt_default\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"hunt-0916-bonus\",\n" +
            "      \"type\": \"treasure-hunt\",\n" +
            "      \"startUtc\": \"2026-09-16T12:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-18T00:00:00Z\",\n" +
            "      \"configKey\": \"hunt_bonus\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"lava-quest-2026-09b\",\n" +
            "      \"type\": \"lava-quest\",\n" +
            "      \"startUtc\": \"2026-09-17T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-09-20T00:00:00Z\",\n" +
            "      \"configKey\": \"lava_quest_v2\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"lava-quest-2026-10\",\n" +
            "      \"type\": \"lava-quest\",\n" +
            "      \"startUtc\": \"2026-10-01T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-10-3\",\n" +
            "      \"configKey\": \"lava_quest_v2\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"id\": \"star-tournament-2026-10\",\n" +
            "      \"type\": \"star-tournament\",\n" +
            "      \"startUtc\": \"2026-10-03T00:00:00Z\",\n" +
            "      \"endUtc\": \"2026-10-06T00:00:00Z\",\n" +
            "      \"configKey\": \"star_tournament_v1\"\n" +
            "    }\n" +
            "  ]\n" +
            "}";

        /// <summary>Dựng lại tài liệu mẫu mỗi lần gọi — test sửa xong không làm fixture của test khác dính theo.</summary>
        public static LiveEventCalendarDocument Document => BuildDocument();

        private static LiveEventCalendarDocument BuildDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey(LiveEventCalendarDocument.DefaultRemoteConfigKey)
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v4"))
                .WithEventType(new LiveEventTypeDefinition("star-tournament", "Giải ngôi sao", 6, false, "star_tournament_v1"))
                .WithEventType(new LiveEventTypeDefinition("treasure-hunt", "Săn kho báu", 7, true, "hunt_default"))
                .WithEventType(new LiveEventTypeDefinition("weekly-pass", "Pass tuần", 1, false, "weekly_pass_s3"))
                // Thứ tự luật: weekly-pass rồi sky-race — khớp 1.601 byte của ExpectedFormat2Json (mục 5.2 luật 7).
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", "pass-", 168, 168, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry(LavaQuestEarlyEntryKey, "lava-quest-2026-09a", "lava-quest",
                    "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                // hunt-0914 KHÔNG tự khai configKey — kế thừa hunt_default của treasure-hunt (mục 6.2).
                .WithFixedEvent(new FixedLiveEventEntry(HuntEarlyEntryKey, "hunt-0914", "treasure-hunt",
                    "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry(HuntBonusEntryKey, "hunt-0916-bonus", "treasure-hunt",
                    "2026-09-16T12:00:00Z", "2026-09-18T00:00:00Z", "hunt_bonus"))
                .WithFixedEvent(new FixedLiveEventEntry(LavaQuestMidEntryKey, "lava-quest-2026-09b", "lava-quest",
                    "2026-09-17T00:00:00Z", "2026-09-20T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry(LavaQuestLateEntryKey, "lava-quest-2026-10", "lava-quest",
                    "2026-10-01T00:00:00Z", "2026-10-3", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry(StarTournamentEntryKey, "star-tournament-2026-10", "star-tournament",
                    "2026-10-03T00:00:00Z", "2026-10-06T00:00:00Z", "star_tournament_v1"))
                .WithPublishedStamp(new PublishedCalendarStamp("2026-09-11T16:20:00Z", PublishedPublisher, PublishedSha256Hex,
                    PublishedByteCount, 2, PublishedNote, PublishedSnapshotJson))
                .Build();
        }

        /// <summary>Tài liệu mẫu nhưng bỏ hết dấu đã đăng — dùng cho test "chưa có bản so" (vá V-2).</summary>
        public static LiveEventCalendarDocument DocumentWithoutPublishedStamp()
        {
            LiveEventCalendarDocument document = Document;
            var builder = new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey(document.RemoteConfigKey);
            for (int index = 0; index < document.EventTypes.Count; index++) builder.WithEventType(document.EventTypes[index]);
            for (int index = 0; index < document.RecurringRules.Count; index++) builder.WithRecurringRule(document.RecurringRules[index]);
            for (int index = 0; index < document.FixedEvents.Count; index++) builder.WithFixedEvent(document.FixedEvents[index]);
            return builder.Build();
        }
    }
}
