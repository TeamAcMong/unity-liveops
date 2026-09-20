namespace DreamTech.LiveOps.Tests
{
    /// <summary>
    /// Tài liệu lịch "XẤU NHẤT" của ma trận kiểm bố cục: chuỗi dài nhất, giờ KHÔNG ĐỌC ĐƯỢC, loại CHƯA KHAI, số lớn, danh
    /// sách rỗng — đúng những trạng thái mà bố cục phải chịu được nhưng chưa màn nào của ma trận từng dựng.
    /// <para>
    /// Vì sao là file RIÊNG chứ không sửa <see cref="LiveOpsDesignSample"/>: 1.601 byte của
    /// <see cref="LiveOpsDesignSample.ExpectedFormat2Json"/> ghim theo đúng tài liệu mẫu, nên thêm một đợt vào mẫu cũ là
    /// làm đỏ nhánh test JSON hoàn toàn không liên quan. Mẫu đẹp giữ nguyên vai trò "ảnh thiết kế"; mẫu này giữ vai trò
    /// "ngày tồi tệ nhất".
    /// </para>
    /// <para>
    /// Vì sao cần: ba lỗi W9-29 (dòng gợi ý mất cụm phím), W9-30 (biểu tượng lỗi bị cắt ở mép) và W9-31 (cột trạng thái
    /// không tên) đều lọt qua 24 màn của ma trận vì cả 24 màn đứng trên tài liệu ĐẸP — không đợt nào bị bỏ, không giờ nào
    /// hỏng, không loại nào chưa khai. Dữ liệu tốt không bao giờ vẽ ra thứ làm vỡ bố cục.
    /// </para>
    /// <para>
    /// Mốc thời gian bám theo <see cref="LiveOpsDesignSample.NowUtc"/> (13/9/2026 08:47 UTC) để trục vẽ được các đợt ở mọi
    /// cỡ cửa sổ của ma trận — một đợt nằm ngoài khoảng ngày trục đang vẽ thì màn không đo được gì về nó.
    /// </para>
    /// </summary>
    public static class LiveOpsWorstCaseSample
    {
        /// <summary>Loại có TÊN HIỂN THỊ dài nhất của mẫu — chỗ vỡ đầu tiên của mọi ô có bề rộng cố định.</summary>
        public const string LongTypeId = "tournament-of-the-eternal-flame-season-twelve";

        public const string LongTypeDisplayName = "Giải đấu Ngọn Lửa Vĩnh Cửu — mùa thứ mười hai (bản thử nghiệm nội bộ)";

        public const string LongTypeConfigKey = "tournament_of_the_eternal_flame_season_twelve_config_v12_internal";

        /// <summary>Loại NGẮN để so sánh trong cùng một bảng: hàng dài và hàng ngắn phải cùng đọc được.</summary>
        public const string ShortTypeId = "lava-quest";

        /// <summary>
        /// Loại mà một đợt TRỎ TỚI nhưng tài liệu CHƯA KHAI. Đây là trạng thái duy nhất làm bảng Loại event vẽ dấu trạng
        /// thái của nó (<c>EventTypeTable.BindStateCell</c> chỉ hiện dấu khi <c>IsDeclared</c> sai) — mẫu đẹp không có loại
        /// nào chưa khai nên cột ấy chưa bao giờ vẽ gì, và đó chính là phiếu W9-31.
        /// </summary>
        public const string UndeclaredTypeId = "mystery-box-blitz-limited-preview";

        public const string LongEntryKey = "entry-worst-eternal-flame-mega-final";

        public const string LongEventId = "tournament-of-the-eternal-flame-season-twelve-2026-09-mega-final-round";

        public const string UndeclaredEntryKey = "entry-worst-undeclared-type";

        public const string UndeclaredEventId = "mystery-box-blitz-limited-preview-2026-09-18-wave-two";

        /// <summary>
        /// Đợt có giờ kết thúc KHÔNG ĐỌC ĐƯỢC. Ô ngày giờ UTC chỉ hiện biểu tượng lỗi ở trạng thái này, và đó là trạng
        /// thái duy nhất bày ra được W9-30 (biểu tượng bị đẩy khỏi hàng rồi mép cửa sổ cắt).
        /// </summary>
        public const string BrokenEndEntryKey = "entry-worst-broken-end";

        public const string BrokenEndEventId = "lava-quest-2026-09-broken-clock";

        /// <summary>Giờ gõ hỏng — cùng KIỂU hỏng với dữ liệu mồi <c>Assets/Demo</c> nhưng KHÁC giá trị, để hai nguồn không lẫn.</summary>
        public const string BrokenEndUtcText = "2026-09-2";

        public const string ShortEntryKey = "entry-worst-short";

        /// <summary>Chu kỳ lớn nhất còn có nghĩa với designer: một năm tính bằng giờ. Ô số phải đọc được cả bốn chữ số.</summary>
        public const int LargePeriodHours = 8760;

        /// <summary>Độ dài một lần chạy dạng ba chữ số — câu luật lặp ghép cả hai số nên nó là câu dài nhất của màn.</summary>
        public const int LargeActiveHours = 999;

        public const string LongIdPrefix = "tournament-of-the-eternal-flame-season-twelve-round-";

        /// <summary>Số byte dạng bảy chữ số — màn Xuất JSON in con số này cạnh sha, đây là chỗ số lớn làm vỡ hàng.</summary>
        public const int LargePublishedByteCount = 1048576;

        public const string LongPublisher = "DatHoUnityDev (máy dựng tự động của đội LiveOps, ca đêm)";

        public const string LongPublishedNote =
            "mở toàn bộ mùa giải Ngọn Lửa Vĩnh Cửu cho nhánh thử nghiệm nội bộ, kèm hai luật lặp mới và một đợt chờ duyệt";

        public const string LongSha256Hex = "5eecb84064b0b21a9d587c3d95e3a471f78c38595216415aba016f3e58702696";

        /// <summary>Dựng lại mỗi lần gọi — một màn sửa tài liệu không được kéo theo màn sau (cùng luật với mẫu đẹp).</summary>
        public static LiveEventCalendarDocument Document
        {
            get { return BuildDocument(); }
        }

        /// <summary>
        /// Tài liệu RỖNG: không loại, không luật, không đợt. Mọi màn phải nói được câu "chưa có gì" mà không để lại một
        /// khung trống cao 0 hay một bảng chỉ còn đường kẻ.
        /// </summary>
        public static LiveEventCalendarDocument EmptyDocument
        {
            get { return LiveEventCalendarDocument.Empty; }
        }

        private static LiveEventCalendarDocument BuildDocument()
        {
            return new LiveEventCalendarDocumentBuilder()
                .WithRemoteConfigKey(LiveEventCalendarDocument.DefaultRemoteConfigKey)
                .WithEventType(new LiveEventTypeDefinition(LongTypeId, LongTypeDisplayName, 3, true, LongTypeConfigKey))
                .WithEventType(new LiveEventTypeDefinition(ShortTypeId, "Nhiệm vụ dung nham", 0, false, "lava_quest_v2"))
                // Luật lặp số lớn: chu kỳ một năm, một lần chạy 999 giờ — câu "cứ 8760 giờ một lần, mỗi đợt chạy 999 giờ"
                // là câu dài nhất mà màn Luật lặp ghép được từ dữ liệu hợp lệ.
                .WithRecurringRule(new RecurringLiveEventRule(LongTypeId, "2026-01-05T00:00:00Z", LongIdPrefix,
                    LargePeriodHours, LargeActiveHours, LongTypeConfigKey))
                .WithRecurringRule(new RecurringLiveEventRule(ShortTypeId, "2026-09-01T00:00:00Z", "lq-", 24, 20, "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry(LongEntryKey, LongEventId, LongTypeId,
                    "2026-09-12T00:00:00Z", "2026-09-15T00:00:00Z", LongTypeConfigKey))
                .WithFixedEvent(new FixedLiveEventEntry(UndeclaredEntryKey, UndeclaredEventId, UndeclaredTypeId,
                    "2026-09-18T00:00:00Z", "2026-09-19T00:00:00Z", string.Empty))
                .WithFixedEvent(new FixedLiveEventEntry(BrokenEndEntryKey, BrokenEndEventId, ShortTypeId,
                    "2026-09-16T00:00:00Z", BrokenEndUtcText, "lava_quest_v2"))
                .WithFixedEvent(new FixedLiveEventEntry(ShortEntryKey, "lq-09", ShortTypeId,
                    "2026-09-14T00:00:00Z", "2026-09-15T00:00:00Z", string.Empty))
                .WithPublishedStamp(new PublishedCalendarStamp("2026-09-11T16:20:00Z", LongPublisher, LongSha256Hex,
                    LargePublishedByteCount, 2, LongPublishedNote, LiveOpsDesignSample.PublishedSnapshotJson))
                .Build();
        }
    }
}
