using System;
using System.Collections.Generic;
using UnityEditor;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Dịch vụ ngôn ngữ của hub (D-L1…D-L3): ngôn ngữ đang dùng, đổi ngôn ngữ, và scope ghim cho test/lệnh chụp. Lựa chọn nhớ
    /// theo MÁY (<see cref="EditorPrefs"/>) chứ không theo project, vì đây là thói quen đọc của người dùng chứ không phải dữ
    /// liệu của game; hub chỉ ghi đúng một khoá <see cref="EditorPreferenceKey"/> và không bao giờ đụng pref nào của Unity.
    /// <para>
    /// Giá trị pref cache lại vì <see cref="LiveOpsHubStringCatalog.Text"/> hỏi <see cref="Current"/> cho MỌI chuỗi khi dựng
    /// cửa sổ — đọc EditorPrefs mỗi lần là hàng nghìn lượt I/O cho một lần vẽ. Ai ghi thẳng vào pref (chỉ có test) phải gọi
    /// <see cref="ReloadFromPreference"/> để cache khỏi nói dối.
    /// </para>
    /// </summary>
    internal static class LiveOpsHubLanguage
    {
        internal const string EditorPreferenceKey = "LiveOpsHub.Language";

        private static readonly LiveOpsHubLanguageId[] AvailableLanguages =
        {
            LiveOpsHubLanguageId.English,
            LiveOpsHubLanguageId.Vietnamese,
        };

        private static LiveOpsHubLanguageScope _innermostScope;
        private static bool _hasCachedPreference;
        private static LiveOpsHubLanguageId _cachedPreference;

        /// <summary>Ngôn ngữ đang đọc chữ: scope ghim (trong cùng) thắng; không có scope thì lấy giá trị pref.</summary>
        internal static LiveOpsHubLanguageId Current
        {
            get { return _innermostScope != null ? _innermostScope.Language : Preference; }
        }

        /// <summary>
        /// Ngôn ngữ đã lưu theo máy, KHÔNG xét scope ghim — dùng để soi đúng đường pref (test) và để <see cref="Set"/> biết có
        /// thật sự đổi gì không. Code dựng UI luôn hỏi <see cref="Current"/>.
        /// </summary>
        internal static LiveOpsHubLanguageId PersistedLanguage
        {
            get { return Preference; }
        }

        /// <summary>Đúng thứ tự mục trong menu chọn ngôn ngữ.</summary>
        internal static IReadOnlyList<LiveOpsHubLanguageId> Available
        {
            get { return AvailableLanguages; }
        }

        /// <summary>Bắn khi người dùng đổi ngôn ngữ (<see cref="Set"/>). Scope ghim KHÔNG bắn sự kiện này.</summary>
        internal static event Action Changed;

        internal static void Set(LiveOpsHubLanguageId language)
        {
            if (!IsKnown(language)) language = LiveOpsHubLanguageId.English;
            if (Preference == language) return;
            _cachedPreference = language;
            _hasCachedPreference = true;
            EditorPrefs.SetString(EditorPreferenceKey, language.ToString());
            Action changed = Changed;
            if (changed != null) changed();
        }

        /// <summary>
        /// Tên gốc của ngôn ngữ — KHÔNG dịch: người đang thấy giao diện tiếng Anh vẫn phải nhận ra dòng tên ngôn ngữ của mình.
        /// Hai tên nằm trong catalog bằng <c>AddShared</c> (một giá trị cho cả hai ngôn ngữ) nên không phải ngoại lệ của lint chữ.
        /// </summary>
        internal static string NativeName(LiveOpsHubLanguageId language)
        {
            return language == LiveOpsHubLanguageId.Vietnamese
                ? LiveOpsHubStrings.LanguageNameVietnamese
                : LiveOpsHubStrings.LanguageNameEnglish;
        }

        /// <summary>Nhãn hai chữ cho menu ở header 26px — chỗ hẹp, không đủ cho tên đầy đủ.</summary>
        internal static string ShortCode(LiveOpsHubLanguageId language)
        {
            return language == LiveOpsHubLanguageId.Vietnamese
                ? LiveOpsHubStrings.LanguageShortCodeVietnamese
                : LiveOpsHubStrings.LanguageShortCodeEnglish;
        }

        internal static LiveOpsHubLanguageScope Override(LiveOpsHubLanguageId language)
        {
            if (!IsKnown(language)) language = LiveOpsHubLanguageId.English;
            LiveOpsHubLanguageScope scope = new LiveOpsHubLanguageScope(language, _innermostScope);
            _innermostScope = scope;
            return scope;
        }

        /// <summary>Đọc lại pref vào cache — chỉ dùng khi có ai ghi thẳng vào EditorPrefs (test giả lập giá trị lạ).</summary>
        internal static void ReloadFromPreference()
        {
            _hasCachedPreference = false;
        }

        internal static void EndScope(LiveOpsHubLanguageScope scope)
        {
            if (scope == null) return;
            if (_innermostScope == scope)
            {
                _innermostScope = scope.Outer;
                scope.Outer = null;
                return;
            }
            // Dispose lệch thứ tự: gỡ đúng scope đó ra khỏi chồng, các scope khác giữ nguyên.
            LiveOpsHubLanguageScope walker = _innermostScope;
            while (walker != null)
            {
                if (walker.Outer == scope)
                {
                    walker.Outer = scope.Outer;
                    scope.Outer = null;
                    return;
                }
                walker = walker.Outer;
            }
        }

        private static LiveOpsHubLanguageId Preference
        {
            get
            {
                if (_hasCachedPreference) return _cachedPreference;
                _cachedPreference = Parse(EditorPrefs.GetString(EditorPreferenceKey, string.Empty));
                _hasCachedPreference = true;
                return _cachedPreference;
            }
        }

        /// <summary>Pref rỗng, rác, hay số ngoài enum đều ra English — chưa chọn bao giờ thì hub nói tiếng Anh, không ném, không log.</summary>
        private static LiveOpsHubLanguageId Parse(string stored)
        {
            if (string.IsNullOrEmpty(stored)) return LiveOpsHubLanguageId.English;
            foreach (LiveOpsHubLanguageId language in AvailableLanguages)
            {
                if (string.Equals(stored, language.ToString(), StringComparison.Ordinal)) return language;
            }
            return LiveOpsHubLanguageId.English;
        }

        private static bool IsKnown(LiveOpsHubLanguageId language)
        {
            foreach (LiveOpsHubLanguageId candidate in AvailableLanguages)
            {
                if (candidate == language) return true;
            }
            return false;
        }
    }
}
