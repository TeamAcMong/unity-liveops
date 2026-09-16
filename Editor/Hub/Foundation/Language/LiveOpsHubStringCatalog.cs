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
    /// Thứ tự tra: ngôn ngữ đang chọn → tiếng Việt (bản gốc, luôn đủ) → tiếng Anh → <c>⟨khoá⟩</c>. Rơi về bản gốc thay vì trả
    /// rỗng vì thiếu chữ là lỗi lập trình nhưng cửa sổ vẫn phải dựng được — người dùng cần thấy chỗ hỏng, không phải một ô trắng.
    /// Test liệt kê mọi khoá chưa dịch; không khoá nào được rơi tới nhánh <c>⟨</c>.
    /// </para>
    /// </summary>
    internal static partial class LiveOpsHubStringCatalog
    {
        internal const string MissingTextOpenMark = "⟨";
        internal const string MissingTextCloseMark = "⟩";

        /// <summary>Bản gốc: ngôn ngữ mọi câu được viết ra lần đầu, và là chỗ tra dự phòng khi ngôn ngữ đang chọn chưa có chữ.</summary>
        internal const LiveOpsHubLanguageId SourceLanguage = LiveOpsHubLanguageId.Vietnamese;

        private static LiveOpsHubStringTable _table;

        internal static string Text(string key)
        {
            LiveOpsHubStringTable table = Table;
            string text;
            if (table.TryGet(LiveOpsHubLanguage.Current, key, out text)) return text;
            if (table.TryGet(SourceLanguage, key, out text)) return text;
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
