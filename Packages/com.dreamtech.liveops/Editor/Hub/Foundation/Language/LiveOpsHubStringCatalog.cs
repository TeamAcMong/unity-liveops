using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nguồn chữ của LiveOps Hub theo ngôn ngữ đang chọn. Mọi thành viên <see cref="LiveOpsHubStrings"/> là một property gọi
    /// <see cref="Text"/> với khoá <c>nameof</c> của chính nó, nên khoá và tên thành viên không bao giờ lệch nhau.
    /// <para>
    /// Chữ nằm trong C#, biên dịch cùng package — không đọc file ngoài lúc chạy. Bảng dựng lazy đúng một lần: mỗi vùng màn hình
    /// đăng ký ở <c>LiveOpsHubStringCatalog.&lt;Vùng&gt;.cs</c> của vùng đó nên hai gói chạy song song không cùng sửa một file.
    /// </para>
    /// <para>
    /// Thứ tự tra (§1.3): ngôn ngữ đang chọn → English (ngôn ngữ mặc định của hub) → ngôn ngữ nào còn chữ → <c>⟨khoá⟩</c>.
    /// Dự phòng là English chứ không phải bản gốc tiếng Việt: người đang đọc tiếng Anh gặp một câu tiếng Việt thì không hiểu,
    /// còn người đọc tiếng Việt gặp câu tiếng Anh thì vẫn lần ra được. Rơi về chữ khác thay vì trả rỗng vì thiếu chữ là lỗi lập
    /// trình nhưng cửa sổ vẫn phải dựng được — người dùng cần thấy chỗ hỏng, không phải một ô trắng. Test chặn mọi khoá thiếu
    /// một trong hai bản, nên nhánh dự phòng chỉ còn là lưới an toàn.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        internal const string MissingTextOpenMark = "⟨";
        internal const string MissingTextCloseMark = "⟩";

        /// <summary>Bản gốc: ngôn ngữ mọi câu được viết ra lần đầu (bản tiếng Anh dịch từ đây).</summary>
        internal const LiveOpsHubLanguageId SourceLanguage = LiveOpsHubLanguageId.Vietnamese;

        /// <summary>Chỗ tra dự phòng khi ngôn ngữ đang chọn chưa có chữ — English vì đó là ngôn ngữ mặc định của hub (§1.3).</summary>
        internal const LiveOpsHubLanguageId FallbackLanguage = LiveOpsHubLanguageId.English;

        private static LiveOpsHubStringTable _table;

        internal static string Text(string key)
        {
            return Resolve(Table, key);
        }

        /// <summary>Đúng thứ tự tra của <see cref="Text"/> nhưng trên một bảng truyền vào — test soi được nhánh dự phòng mà
        /// không phải làm hỏng bảng thật đang dựng cửa sổ.</summary>
        internal static string Resolve(LiveOpsHubStringTable table, string key)
        {
            if (table == null) return MissingTextOpenMark + key + MissingTextCloseMark;
            string text;
            if (table.TryGet(LiveOpsHubLanguage.Current, key, out text)) return text;
            if (table.TryGet(FallbackLanguage, key, out text)) return text;
            foreach (LiveOpsHubLanguageId language in LiveOpsHubLanguage.Available)
            {
                if (table.TryGet(language, key, out text)) return text;
            }
            return MissingTextOpenMark + key + MissingTextCloseMark;
        }

        /// <summary>Khoá theo thứ tự đăng ký — test phủ khoá và bảng đối chiếu bản dịch duyệt từ đây.</summary>
        internal static IReadOnlyList<string> Keys
        {
            get { return Table.Keys; }
        }

        /// <summary>Chữ của đúng một ngôn ngữ, KHÔNG rơi về bản gốc — test dùng để biết khoá nào chưa dịch.</summary>
        internal static bool TryGetExact(LiveOpsHubLanguageId language, string key, out string text)
        {
            return Table.TryGet(language, key, out text);
        }

        internal static bool IsShared(string key)
        {
            return Table.IsShared(key);
        }

        /// <summary>Dựng một bảng mới từ chính các hàm đăng ký — test khoá trùng gọi để không đụng bảng đang dùng.</summary>
        internal static LiveOpsHubStringTable BuildTable()
        {
            LiveOpsHubStringTable table = new LiveOpsHubStringTable();
            RegisterFoundation(table);
            RegisterKit(table);
            RegisterShell(table);
            RegisterFeedback(table);
            RegisterControls(table);
            RegisterTimelineModel(table);
            RegisterTimeline(table);
            RegisterServices(table);
            RegisterFindings(table);
            RegisterExportGate(table);
            RegisterPreview(table);
            return table;
        }

        private static LiveOpsHubStringTable Table
        {
            get
            {
                if (_table == null) _table = BuildTable();
                return _table;
            }
        }

        static partial void RegisterFoundation(LiveOpsHubStringTable table);
        static partial void RegisterKit(LiveOpsHubStringTable table);
        static partial void RegisterShell(LiveOpsHubStringTable table);
        static partial void RegisterFeedback(LiveOpsHubStringTable table);
        static partial void RegisterControls(LiveOpsHubStringTable table);
        static partial void RegisterTimelineModel(LiveOpsHubStringTable table);
        static partial void RegisterTimeline(LiveOpsHubStringTable table);
        static partial void RegisterServices(LiveOpsHubStringTable table);
        static partial void RegisterFindings(LiveOpsHubStringTable table);
        static partial void RegisterExportGate(LiveOpsHubStringTable table);
        static partial void RegisterPreview(LiveOpsHubStringTable table);
    }
}
