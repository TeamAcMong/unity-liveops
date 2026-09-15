using System;
using DreamTech.LiveOps.Unity;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Dữ liệu cho mục menu "Hiện dữ liệu mẫu (chỉ để xem giao diện)" (mục 8): tài liệu mẫu 13/9/2026 08:47, đồng hồ đứng yên
    /// ở giờ đó, giờ máy +7 và một asset lịch chỉ sống trong bộ nhớ.
    /// <para>
    /// Vì sao có bản thứ hai của <c>LiveOpsDesignSample</c>: fixture đó nằm trong assembly test (<c>UNITY_INCLUDE_TESTS</c>), không
    /// đi theo bản cài của game, nên menu của hub không gọi được. Hai bản bị khoá bằng test
    /// <c>PreviewSampleTests.PreviewSample_EqualsDesignSampleFixture</c> — sửa một bên mà quên bên kia thì test đỏ, ảnh chụp
    /// và cửa sổ mẫu không trôi khỏi nhau.
    /// </para>
    /// <para>
    /// Lớp này không biết phiên, cửa sổ hay services: nó chỉ trao nguyên liệu, để cửa sổ mẫu (G-SHELLPOLISH) tự lắp và để test
    /// kiểm được mà không dựng hub.
    /// </para>
    /// </summary>
    internal static class LiveOpsHubPreviewSample
    {
        /// <summary>BÂY GIỜ của kịch bản mẫu — mọi chữ "đang chạy / còn 2 ngày" trong thiết kế tính từ mốc này.</summary>
        public static readonly DateTime NowUtc = new DateTime(2026, 9, 13, 8, 47, 0, DateTimeKind.Utc);

        /// <summary>Giờ máy của Editor trong kịch bản mẫu — UTC+7, để chữ "giờ máy" không phụ thuộc máy đang mở.</summary>
        public static readonly TimeSpan DeviceOffset = TimeSpan.FromHours(7);

        /// <summary>
        /// Tên asset trong bộ nhớ. Không đặt "Main" như asset thật: log hay Inspector nhắc tên này thì người đọc biết ngay đó là
        /// bản xem thử, không phải lịch của game.
        /// </summary>
        public const string AssetName = "LiveOpsHubPreviewSample";

        private const string LavaQuestEarlyEntryKey = "entry-lava-quest-2026-09a";
        private const string HuntEarlyEntryKey = "entry-hunt-0914";
        private const string HuntBonusEntryKey = "entry-hunt-0916-bonus";
        private const string LavaQuestMidEntryKey = "entry-lava-quest-2026-09b";
        private const string LavaQuestLateEntryKey = "entry-lava-quest-2026-10";
        private const string StarTournamentEntryKey = "entry-star-tournament-2026-10";

        private const string PublishedUtcText = "2026-09-11T16:20:00Z";
        private const string PublishedPublisher = "DatHoUnityDev";
        private const string PublishedNote = "mở lava-quest tháng 9";
        private const int PublishedByteCount = 1425;
        private const int PublishedFormatVersion = 2;
        private const string PublishedSha256Hex = "5eecb84064b0b21a9d587c3d95e3a471f78c38595216415aba016f3e58702696";

        // Đúng byte bản đã đăng 11/9 16:20 (định dạng 2, không LF cuối) — khác nháp đúng 5 chỗ của diff mẫu (mục 6.3). Giữ
        // nguyên văn thay vì cho bộ ghi JSON sinh lại: sha và số byte của dấu là thứ đã đăng thật, bộ ghi đổi định dạng về sau
        // không được làm dấu mẫu tự đổi theo.
        private static readonly string PublishedSnapshotJson =
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
        /// Dựng lại tài liệu mỗi lần gọi — cửa sổ mẫu sửa thử xong mở lại vẫn thấy đúng mẫu, và test không dính trạng thái của
        /// nhau.
        /// </summary>
        public static LiveEventCalendarDocument Document => BuildDocument();

        /// <summary>Đồng hồ đứng yên ở <see cref="NowUtc"/>: cửa sổ mẫu mở lúc nào cũng thấy cùng một khung "đang chạy".</summary>
        public static ManualLiveOpsClock CreateClock()
        {
            return new ManualLiveOpsClock(NowUtc);
        }

        public static ManualLiveOpsHubTimeZone CreateTimeZone()
        {
            return new ManualLiveOpsHubTimeZone(DeviceOffset);
        }

        /// <summary>
        /// Asset lịch chứa <see cref="Document"/>, tạo bằng <c>CreateInstance</c> và gắn <see cref="HideFlags.DontSave"/> TRƯỚC khi
        /// nạp dữ liệu. Vì sao: asset không có đường dẫn nên <c>AssetDatabase.SaveAssets</c> không ghi được, còn
        /// <c>DontSave</c> chặn nốt đường lọt qua scene/prefab khi có ai lỡ gán tham chiếu — dữ liệu mẫu không bao giờ ra đĩa.
        /// Không ghi Undo: sửa thử trên bản mẫu không được chen vào lịch sử Undo của asset thật đang mở ở cửa sổ khác (R-24).
        /// <para>Người gọi sở hữu asset và phải <c>Object.DestroyImmediate</c> khi đóng cửa sổ — <c>DontSave</c> gồm cả
        /// <c>DontUnloadUnusedAsset</c> nên Unity không tự dọn.</para>
        /// </summary>
        public static LiveEventCalendarAsset CreateAsset()
        {
            LiveEventCalendarAsset asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
            asset.hideFlags = HideFlags.DontSave;
            asset.name = AssetName;
            asset.ApplyDocument(BuildDocument());
            return asset;
        }

        private static LiveEventCalendarDocument BuildDocument()
        {
            // Thứ tự loại, luật, đợt giữ đúng fixture: thứ tự luật quyết định byte JSON xuất (weekly-pass rồi sky-race, V-12), thứ
            // tự đợt quyết định dòng timeline và chỉ số hàng trong ảnh chụp.
            return new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey(LiveEventCalendarDocument.DefaultRemoteConfigKey)
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v4"))
                .WithEventType(new LiveEventTypeDefinition("star-tournament", "Giải ngôi sao", 6, false, "star_tournament_v1"))
                .WithEventType(new LiveEventTypeDefinition("treasure-hunt", "Săn kho báu", 7, true, "hunt_default"))
                .WithEventType(new LiveEventTypeDefinition("weekly-pass", "Pass tuần", 1, false, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", "2026-01-05T00:00:00Z", "pass-", 168, 168, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", "2026-01-05T00:00:00Z", "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry(LavaQuestEarlyEntryKey, "lava-quest-2026-09a", "lava-quest",
                    "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                // hunt-0914 không tự khai configKey — kế thừa hunt_default, để luật config-key-missing có đúng một ca mẫu (6.2).
                .WithFixedEvent(new FixedLiveEventEntry(HuntEarlyEntryKey, "hunt-0914", "treasure-hunt",
                    "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry(HuntBonusEntryKey, "hunt-0916-bonus", "treasure-hunt",
                    "2026-09-16T12:00:00Z", "2026-09-18T00:00:00Z", "hunt_bonus"))
                .WithFixedEvent(new FixedLiveEventEntry(LavaQuestMidEntryKey, "lava-quest-2026-09b", "lava-quest",
                    "2026-09-17T00:00:00Z", "2026-09-20T00:00:00Z", "lava_quest_v2"))
                // Giờ kết thúc gõ hỏng giữ nguyên văn (PD-2) — đây là ca "giờ kết thúc sai định dạng" mà thiết kế trình bày.
                .WithFixedEvent(new FixedLiveEventEntry(LavaQuestLateEntryKey, "lava-quest-2026-10", "lava-quest",
                    "2026-10-01T00:00:00Z", "2026-10-3", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry(StarTournamentEntryKey, "star-tournament-2026-10", "star-tournament",
                    "2026-10-03T00:00:00Z", "2026-10-06T00:00:00Z", "star_tournament_v1"))
                .WithPublishedStamp(new PublishedCalendarStamp(PublishedUtcText, PublishedPublisher, PublishedSha256Hex,
                    PublishedByteCount, PublishedFormatVersion, PublishedNote, PublishedSnapshotJson))
                .Build();
        }
    }
}
