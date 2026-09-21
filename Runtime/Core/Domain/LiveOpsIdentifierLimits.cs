namespace DreamTech.LiveOps
{
    /// <summary>
    /// GIỚI HẠN ĐỘ DÀI của dữ liệu lịch — hợp đồng SOẠN THẢO, không phải luật chặn lúc chạy.
    /// <para>
    /// Vì sao cần: trước W11 lõi chỉ có luật KÝ TỰ (<see cref="LiveEventInstance.ValidateIdentifier"/>: không rỗng, không
    /// chứa <see cref="LiveEventInstance.ReservedSeparator"/>, không xuống dòng) mà không có luật ĐỘ DÀI nào. Thiếu con số,
    /// mọi câu hỏi kiểu "bố cục phải chịu được id dài tới đâu" chỉ trả lời được bằng cảm tính, và bộ dữ liệu "xấu nhất" của
    /// cổng bố cục thành một chuỗi dài tuỳ hứng — đo xong không biết con số ấy có nghĩa gì.
    /// </para>
    /// <para>
    /// Vì sao KHÔNG ném lỗi khi vượt: bất biến của package nói dữ liệu xấu từ xa (remote config, YAML sửa tay) không được
    /// làm đường chạy của game ném — mục hỏng thì bị bỏ và ghi lý do. Thêm một cửa ném mới ở đây sẽ làm đúng cái nó cấm, và
    /// còn là thay đổi phá vỡ với game đã đặt id dài hơn. Nên đây là HẰNG CÓ TÀI LIỆU: bộ dữ liệu xấu nhất của cổng bố cục
    /// dựng đúng theo nó, README khai nó cho người soạn lịch. Một luật kiểm cảnh báo khi vượt là việc của đợt sau — khai công
    /// khai ở README mục 4.5 và ở mục "Chưa có ở bản này" của CHANGELOG, không giấu.
    /// </para>
    /// <para>
    /// Các con số dưới đây suy ra từ hai chỗ, không phải chọn cho đẹp: (1) dữ liệu người thật đã soạn trong repo này — mục
    /// dài nhất của lịch mẫu <c>Assets/Demo/LiveOps/Calendars/Main.asset</c> là khoá mục
    /// <c>entry-star-tournament-2026-10</c> (29 ký tự), tên hiển thị dài nhất 28 ký tự; (2) phép ghép khoá của chính package.
    /// </para>
    /// </summary>
    public static class LiveOpsIdentifierLimits
    {
        /// <summary>
        /// Số ký tự tối đa của một ĐỊNH DANH: id đợt, id loại event, khoá mục cố định, config key, khoá nhận quà, system id.
        /// <para>
        /// Vì sao 64: id đợt và khoá nhận quà ghép thành grant id <c>liveops.claim#&lt;eventId&gt;#&lt;claimKey&gt;</c> —
        /// xem <see cref="MaxGrantIdLength"/>. Ở mức 64 mỗi vế, grant id dài nhất là 143 ký tự, còn dư so với mốc 255 mà các
        /// kho khoá-giá trị game hay dùng để lưu grant id đã phát (PlayerPrefs nền registry, khoá tham số remote config)
        /// nhận được. 64 cũng là hơn HAI LẦN mục dài nhất mà người thật đã soạn trong repo (29 ký tự), tức còn chỗ cho quy
        /// ước đặt tên dài hơn hẳn hôm nay mà không phải bump lại con số này.
        /// </para>
        /// </summary>
        public const int MaxIdentifierLength = 64;

        /// <summary>
        /// Bề rộng chữ của số thứ tự lần lặp mà lịch lặp ghép vào sau tiền tố — <c>long.MinValue</c> viết ra là
        /// "-9223372036854775808", đúng 20 ký tự. Đây là số ĐO ĐƯỢC của kiểu dữ liệu, không phải con số chọn tay.
        /// </summary>
        public const int MaxOccurrenceIndexLength = 20;

        /// <summary>
        /// Số ký tự tối đa của TIỀN TỐ ID luật lặp. Suy ra, không chọn: id của một lần lặp là tiền tố + số thứ tự
        /// (<c>LiveEventCalendarCompiler.IsOccurrenceIdOf</c>), nên tiền tố dài hơn
        /// <see cref="MaxIdentifierLength"/> − <see cref="MaxOccurrenceIndexLength"/> sẽ sinh ra id đợt vượt giới hạn dù
        /// bản thân tiền tố trông vẫn ngắn — đúng loại bẫy mà một hằng suy ra sẽ chặn được còn hai hằng rời rạc thì không.
        /// </summary>
        public const int MaxRecurringIdPrefixLength = MaxIdentifierLength - MaxOccurrenceIndexLength;

        /// <summary>
        /// Số ký tự tối đa của một TÊN HIỂN THỊ do người đặt: tên loại event, tên người đăng bản lịch.
        /// <para>
        /// Vì sao cùng 64 với định danh: tên hiển thị và định danh đứng cạnh nhau trong cùng một hàng bảng của hub, nên hai
        /// ngân sách khác nhau chỉ làm cột này phải rộng hơn cột kia mà không đổi được điều gì. 64 cũng hơn hai lần tên dài
        /// nhất người thật đã đặt trong repo (28 ký tự).
        /// </para>
        /// </summary>
        public const int MaxDisplayNameLength = 64;

        /// <summary>
        /// Số ký tự tối đa của một GHI CHÚ một dòng: ghi chú của dấu "đã đăng", ghi chú "bỏ qua cảnh báo".
        /// <para>
        /// Vì sao 160: ghi chú là MỘT câu kể lại một lần đăng, cùng vai với dòng tiêu đề của một commit — quy ước lâu đời
        /// cho dòng ấy là 72 ký tự. Lấy gấp đôi rồi làm tròn lên bội của 32 ra 160: đủ cho một câu tiếng Việt có dấu kể đủ
        /// việc, mà vẫn là MỘT dòng chứ không mở đường cho cả đoạn văn (đoạn văn thì chỗ của nó là hệ thống ticket, không
        /// phải một field của asset lịch).
        /// </para>
        /// </summary>
        public const int MaxNoteLength = 160;

        /// <summary>
        /// Số ký tự tối đa của TÊN ASSET lịch (phần tên, không kèm đuôi ".asset").
        /// <para>
        /// Vì sao 64: tên asset là một đoạn đường dẫn trên đĩa, mà mỗi đoạn đường dẫn của APFS/NTFS chỉ nhận 255 byte —
        /// một tên tiếng Việt có dấu tốn tới 3 byte/ký tự ở UTF-8, nên 64 ký tự là 192 byte, cộng ".asset" vẫn dưới mốc.
        /// Hub in tên này trong câu phát hiện của màn Kiểm lịch nên nó cũng là chuỗi hiển thị, giữ cùng ngân sách với
        /// <see cref="MaxDisplayNameLength"/> cho dễ nhớ.
        /// </para>
        /// </summary>
        public const int MaxAssetNameLength = 64;

        /// <summary>
        /// Độ dài lớn nhất của grant id nhận quà <c>liveops.claim#&lt;eventId&gt;#&lt;claimKey&gt;</c> khi cả hai vế chạm
        /// <see cref="MaxIdentifierLength"/>. Đây là con số để đối chiếu với giới hạn khoá của kho lưu bên game, và là lý do
        /// <see cref="MaxIdentifierLength"/> dừng ở 64 chứ không lấy tròn 128.
        /// </summary>
        public static readonly int MaxGrantIdLength =
            LiveOpsSystem.ClaimGrantPrefix.Length + MaxIdentifierLength + SeparatorLength + MaxIdentifierLength;

        /// <summary>Một <see cref="LiveEventInstance.ReservedSeparator"/> là một ký tự — đặt tên để phép cộng ở trên đọc ra nghĩa.</summary>
        private const int SeparatorLength = 1;
    }
}
