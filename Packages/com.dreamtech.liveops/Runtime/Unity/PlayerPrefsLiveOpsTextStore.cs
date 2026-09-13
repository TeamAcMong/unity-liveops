using UnityEngine;

namespace DreamTech.LiveOps.Unity
{
    /// <summary>
    /// Lưu trạng thái live-ops vào PlayerPrefs. Dùng được ngay khi game chưa muốn đụng save system riêng; đổi sang save của game
    /// (hay cloud save) chỉ là cắm một <see cref="ILiveOpsTextStore"/> khác.
    /// </summary>
    public sealed class PlayerPrefsLiveOpsTextStore : ILiveOpsTextStore
    {
        public const string DefaultKeyPrefix = "dreamtech.liveops.";

        private readonly string _keyPrefix;

        public PlayerPrefsLiveOpsTextStore(string keyPrefix = DefaultKeyPrefix)
        {
            _keyPrefix = keyPrefix ?? string.Empty;
        }

        public bool TryRead(string key, out string value)
        {
            value = PlayerPrefs.GetString(_keyPrefix + key, string.Empty);
            return !string.IsNullOrEmpty(value);
        }

        public void Write(string key, string value)
        {
            PlayerPrefs.SetString(_keyPrefix + key, value ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void Delete(string key)
        {
            PlayerPrefs.DeleteKey(_keyPrefix + key);
            PlayerPrefs.Save();
        }
    }
}
