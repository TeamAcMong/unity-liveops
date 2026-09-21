using System;
using System.Collections.Generic;
using DreamTech.LiveOps.Unity;
using UnityEditor;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Tìm và nhớ asset lịch đang mở (PD-16). Nhớ bằng GUID trong <c>EditorUserSettings</c> — theo project + người dùng, lưu ở
    /// <c>UserSettings/EditorUserSettings.asset</c>, sống qua khởi động lại và qua xoá <c>Library/</c> ở hai bản (SP-14 kiểm chứng) —
    /// không EditorPrefs (theo máy, lẫn giữa các project) và không SessionState (mất khi đóng Unity). Không nhớ bằng instance ID.
    /// </summary>
    internal sealed class LiveOpsHubAssetLocator
    {
        public const string CalendarAssetGuidKey = "DreamTech.LiveOps.Hub.CalendarAssetGuid";

        private const string AssetSearchFilter = "t:" + nameof(LiveEventCalendarAsset);

        /// <param name="settingsKey">Khoá EditorUserSettings; test truyền khoá riêng để không đè asset người dùng đang mở.</param>
        public LiveOpsHubAssetLocator(string settingsKey = CalendarAssetGuidKey)
        {
            if (string.IsNullOrEmpty(settingsKey)) throw new ArgumentNullException(nameof(settingsKey));
            SettingsKey = settingsKey;
        }

        public string SettingsKey { get; }

        /// <summary>GUID đã nhớ; "" khi chưa nhớ.</summary>
        public string RememberedGuid => EditorUserSettings.GetConfigValue(SettingsKey) ?? string.Empty;

        /// <summary>
        /// Asset đã nhớ nếu còn; không thì asset đầu tiên theo đường dẫn (thứ tự ổn định giữa các lần mở) và nhớ luôn nó — mở hub lần
        /// đầu trong project có một lịch không bắt người dùng chọn. null khi project chưa có lịch nào.
        /// </summary>
        public LiveEventCalendarAsset FindRemembered()
        {
            string guid = RememberedGuid;
            if (guid.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LiveEventCalendarAsset remembered = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<LiveEventCalendarAsset>(path);
                if (remembered != null) return remembered;
            }

            IReadOnlyList<string> paths = FindAssetPaths();
            foreach (string path in paths)
            {
                LiveEventCalendarAsset candidate = AssetDatabase.LoadAssetAtPath<LiveEventCalendarAsset>(path);
                if (candidate == null) continue;
                Remember(candidate);
                return candidate;
            }
            return null;
        }

        /// <summary>Nhớ asset có trên đĩa; asset chỉ trong bộ nhớ (không GUID) thì bỏ qua — không có gì để tìm lại lần sau.</summary>
        public void Remember(LiveEventCalendarAsset asset)
        {
            if (asset == null) return;
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return;
            string guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid) || string.Equals(guid, RememberedGuid, StringComparison.Ordinal)) return;
            EditorUserSettings.SetConfigValue(SettingsKey, guid);
        }

        public void Forget()
        {
            EditorUserSettings.SetConfigValue(SettingsKey, null);
        }

        /// <summary>HelpBox "Có 2 LiveEventCalendarAsset trong project" (7.1).</summary>
        public int CountAssets()
        {
            return FindAssetPaths().Count;
        }

        /// <summary>Đường dẫn mọi asset lịch trong project (Assets + Packages), sắp theo thứ tự chữ để lựa chọn mặc định ổn định.</summary>
        public IReadOnlyList<string> FindAssetPaths()
        {
            string[] guids = AssetDatabase.FindAssets(AssetSearchFilter);
            List<string> paths = new List<string>(guids.Length);
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && seen.Add(path)) paths.Add(path);
            }
            paths.Sort(StringComparer.Ordinal);
            return paths;
        }
    }
}
