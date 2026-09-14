using System;
using DreamTech.LiveOps.Unity;
using UnityEditor;
using UnityEngine;

namespace DreamTech.LiveOps.Demo.EditorTools
{
    /// <summary>
    /// Sinh <see cref="AssetPath"/> — asset lịch mẫu của demo, đúng dữ liệu mẫu của kế hoạch 0.2.0 (5 loại, 2 luật lặp, 6 đợt cố
    /// định, 1 dấu đã đăng; bằng <c>LiveOpsDesignSample</c> của test package). Asset là sản phẩm sinh ra: sửa dữ liệu ở đây rồi dựng
    /// lại, đừng sửa tay trong asset.
    ///
    /// <para>Chạy từ menu hoặc batchmode:
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod DreamTech.LiveOps.Demo.EditorTools.LiveOpsDemoCalendarAssetBuilder.BuildFromCommandLine</c></para>
    ///
    /// <para>Vì sao dữ liệu chép lại thay vì gọi <c>LiveOpsDesignSample</c>: fixture đó nằm trong assembly test (chỉ Editor, cần
    /// <c>UNITY_INCLUDE_TESTS</c>), demo không được phụ thuộc test. Để hai bản không lệch im lặng, builder tự kiểm số byte + SHA-256
    /// của JSON nháp và của bản đã đăng với đúng hằng mà test package khoá — lệch thì ném, lệnh dòng lệnh thoát 1.
    ///
    /// <para><b>Phạm vi phép kiểm này chỉ tới những gì thật sự xuất ra JSON</b> — luật lặp (idPrefix/anchor/period/active/configKey)
    /// và đợt cố định (id/type/start/end/configKey). Các field KHÔNG vào JSON — <c>displayName</c>/<c>colorSlot</c>/<c>requiresJoin</c>
    /// của <see cref="LiveEventTypeDefinition"/>, <c>entryKey</c> của đợt cố định, <c>publisher</c>/<c>note</c>/<c>publishedUtc</c> của
    /// dấu đã đăng — không đi qua hash nên có thể lệch <c>LiveOpsDesignSample</c> mà builder không báo; sửa các field đó ở
    /// <see cref="AddEventTypes"/>/<see cref="CreateSampleDocument"/>/<see cref="CreatePublishedDocument"/> thì phải tự đối chiếu tay
    /// với <c>LiveOpsDesignSample.Document</c> (Packages/com.dreamtech.liveops/Tests/Editor/Support).</para>
    /// </summary>
    public static class LiveOpsDemoCalendarAssetBuilder
    {
        public const string AssetPath = CalendarsFolderPath + "/Main.asset";

        private const string DemoFolderPath = "Assets/Demo";
        private const string LiveOpsFolderName = "LiveOps";
        private const string LiveOpsFolderPath = DemoFolderPath + "/" + LiveOpsFolderName;
        private const string CalendarsFolderName = "Calendars";
        private const string CalendarsFolderPath = LiveOpsFolderPath + "/" + CalendarsFolderName;

        // Hằng khoá của dữ liệu mẫu (mục 0.3/5.3 kế hoạch, LiveEventCalendarJsonWriterTests + LiveOpsDesignSample).
        private const int ExpectedDraftByteCount = 1601;
        private const string ExpectedDraftSha256Hex = "da4f831d840a21b7b4c08f9d7ffdb515f4ba89cdea7fae2ca8cde03e8d6c7646";
        private const int ExpectedPublishedByteCount = 1425;
        private const string ExpectedPublishedSha256Hex = "5eecb84064b0b21a9d587c3d95e3a471f78c38595216415aba016f3e58702696";

        private const string PublishedUtcText = "2026-09-11T16:20:00Z";
        private const string Publisher = "DatHoUnityDev";
        private const string PublishedNote = "mở lava-quest tháng 9";

        private const string AnchorUtcText = "2026-01-05T00:00:00Z";

        [MenuItem("Tools/DreamTech/LiveOps/Demo/Build Sample Calendar Asset")]
        public static void Build()
        {
            BuildAsset();
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                BuildAsset();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Tạo asset nếu chưa có, không thì ghi đè field của asset ĐANG CÓ. Không xoá rồi tạo lại: GUID trong <c>.meta</c> phải giữ
        /// nguyên vì scene demo tham chiếu asset theo GUID — và dựng hai lần phải ra cùng từng byte.
        /// </summary>
        public static LiveEventCalendarAsset BuildAsset()
        {
            LiveEventCalendarDocument document = CreateSampleDocument();
            VerifyMatchesDesignSample(document);

            EnsureFolder(DemoFolderPath, LiveOpsFolderName);
            EnsureFolder(LiveOpsFolderPath, CalendarsFolderName);

            LiveEventCalendarAsset asset = AssetDatabase.LoadAssetAtPath<LiveEventCalendarAsset>(AssetPath);
            if (asset == null)
            {
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(AssetPath, AssetPathToGUIDOptions.OnlyExistingAssets)))
                {
                    // Có file nhưng không đọc được thành asset lịch (script mất, sửa tay hỏng): ghi đè sẽ đổi GUID và làm scene mất
                    // tham chiếu im lặng — dừng để người dùng tự xem.
                    throw new InvalidOperationException(AssetPath + " đã có nhưng không phải LiveEventCalendarAsset — xoá hoặc sửa tay rồi dựng lại.");
                }
                asset = ScriptableObject.CreateInstance<LiveEventCalendarAsset>();
                asset.ApplyDocument(document);
                AssetDatabase.CreateAsset(asset, AssetPath);
            }
            else
            {
                asset.ApplyDocument(document);
                EditorUtility.SetDirty(asset);
            }

            AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log("[LiveOps Demo] Đã dựng " + AssetPath);
            return asset;
        }

        /// <summary>Nháp hiện tại của dữ liệu mẫu — xuất định dạng 2 ra đúng <c>plan/sample_format2.json</c>.</summary>
        private static LiveEventCalendarDocument CreateSampleDocument()
        {
            LiveEventCalendarJsonText published = LiveEventCalendarJsonWriter.Write(CreatePublishedDocument(), LiveEventCalendarJsonFormat.Version2);

            return AddEventTypes(new LiveEventCalendarDocumentBuilder())
                // Thứ tự luật: weekly-pass rồi sky-race — thứ tự luật trong JSON là thứ tự asset (luật 7, mục 5.2).
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", AnchorUtcText, "pass-", 168, 168, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", AnchorUtcText, "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-quest-2026-09a", "lava-quest-2026-09a", "lava-quest",
                    "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                // hunt-0914 KHÔNG tự khai configKey — kế thừa hunt_default của treasure-hunt (mục 6.2).
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-0914", "hunt-0914", "treasure-hunt",
                    "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-0916-bonus", "hunt-0916-bonus", "treasure-hunt",
                    "2026-09-16T12:00:00Z", "2026-09-18T00:00:00Z", "hunt_bonus"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-quest-2026-09b", "lava-quest-2026-09b", "lava-quest",
                    "2026-09-17T00:00:00Z", "2026-09-20T00:00:00Z", "lava_quest_v2"))
                // Giờ gõ hỏng giữ nguyên văn (PD-2) — mẫu cần một mục bị bỏ vì giờ sai để hub và demo có cái mà báo.
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-quest-2026-10", "lava-quest-2026-10", "lava-quest",
                    "2026-10-01T00:00:00Z", "2026-10-3", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-star-tournament-2026-10", "star-tournament-2026-10", "star-tournament",
                    "2026-10-03T00:00:00Z", "2026-10-06T00:00:00Z", "star_tournament_v1"))
                .WithPublishedStamp(new PublishedCalendarStamp(PublishedUtcText, Publisher, published.Sha256Hex, published.ByteCount,
                    (int)LiveEventCalendarJsonFormat.Version2, PublishedNote, published.Text))
                .Build();
        }

        /// <summary>
        /// Bản đã đăng 11/9 16:20 — khác nháp đúng 5 chỗ của diff mẫu (mục 6.3): tiền tố weekly-pass còn "weekly-pass-", hunt-0914
        /// còn configKey hunt_v1, lava-quest-2026-09b còn kết thúc 19/9, lava-quest-2026-10 còn giờ kết thúc chuẩn, chưa có
        /// hunt-0916-bonus. Dấu đã đăng lấy byte/SHA từ chính JSON ghi ra thay vì chép chuỗi 1.425 byte — một nguồn, không lệch.
        /// </summary>
        private static LiveEventCalendarDocument CreatePublishedDocument()
        {
            return AddEventTypes(new LiveEventCalendarDocumentBuilder())
                .WithRecurringRule(new RecurringLiveEventRule("weekly-pass", AnchorUtcText, "weekly-pass-", 168, 168, "weekly_pass_s3"))
                .WithRecurringRule(new RecurringLiveEventRule("sky-race", AnchorUtcText, "sky-race-", 24, 20, "sky_race_v4"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-quest-2026-09a", "lava-quest-2026-09a", "lava-quest",
                    "2026-09-10T00:00:00Z", "2026-09-13T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-hunt-0914", "hunt-0914", "treasure-hunt",
                    "2026-09-14T00:00:00Z", "2026-09-17T00:00:00Z", "hunt_v1"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-quest-2026-09b", "lava-quest-2026-09b", "lava-quest",
                    "2026-09-17T00:00:00Z", "2026-09-19T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-lava-quest-2026-10", "lava-quest-2026-10", "lava-quest",
                    "2026-10-01T00:00:00Z", "2026-10-03T00:00:00Z", "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry("entry-star-tournament-2026-10", "star-tournament-2026-10", "star-tournament",
                    "2026-10-03T00:00:00Z", "2026-10-06T00:00:00Z", "star_tournament_v1"))
                .Build();
        }

        private static LiveEventCalendarDocumentBuilder AddEventTypes(LiveEventCalendarDocumentBuilder builder)
        {
            return builder
                .WithRemoteConfigKey(LiveEventCalendarDocument.DefaultRemoteConfigKey)
                .WithEventType(new LiveEventTypeDefinition("lava-quest", "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                .WithEventType(new LiveEventTypeDefinition("sky-race", "Đua trên trời", 4, false, "sky_race_v4"))
                .WithEventType(new LiveEventTypeDefinition("star-tournament", "Giải ngôi sao", 6, false, "star_tournament_v1"))
                // requiresJoin = true: WithEventTypesFrom đăng ký treasure-hunt là ExplicitJoin — test PlayMode của đợt săn dựa vào.
                .WithEventType(new LiveEventTypeDefinition("treasure-hunt", "Săn kho báu", 7, true, "hunt_default"))
                .WithEventType(new LiveEventTypeDefinition("weekly-pass", "Pass tuần", 1, false, "weekly_pass_s3"));
        }

        private static void VerifyMatchesDesignSample(LiveEventCalendarDocument document)
        {
            LiveEventCalendarJsonText draft = LiveEventCalendarJsonWriter.Write(document, LiveEventCalendarJsonFormat.Version2);
            VerifyBytes("JSON nháp", draft.ByteCount, draft.Sha256Hex, ExpectedDraftByteCount, ExpectedDraftSha256Hex);

            PublishedCalendarStamp stamp = document.PublishedStamps[0];
            VerifyBytes("bản đã đăng", stamp.ByteCount, stamp.Sha256Hex, ExpectedPublishedByteCount, ExpectedPublishedSha256Hex);
        }

        private static void VerifyBytes(string what, int byteCount, string sha256Hex, int expectedByteCount, string expectedSha256Hex)
        {
            if (byteCount == expectedByteCount && string.Equals(sha256Hex, expectedSha256Hex, StringComparison.Ordinal)) return;
            throw new InvalidOperationException("Dữ liệu mẫu của demo lệch LiveOpsDesignSample ở " + what + ": " + byteCount + " byte, sha " +
                                                sha256Hex + " (cần " + expectedByteCount + " byte, sha " + expectedSha256Hex + ").");
        }

        private static void EnsureFolder(string parentPath, string folderName)
        {
            if (AssetDatabase.IsValidFolder(parentPath + "/" + folderName)) return;
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parentPath, folderName)))
            {
                throw new InvalidOperationException("Không tạo được thư mục " + parentPath + "/" + folderName + ".");
            }
        }
    }
}
