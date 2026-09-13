using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Bản ghi khoá=giá trị mã hoá thành chuỗi nhiều dòng. Không dùng JSON vì assembly này không tham chiếu Unity hay thư viện
    /// ngoài; định dạng dòng đọc được bằng mắt khi debug PlayerPrefs. Mỗi bản ghi có <c>format</c> để đổi định dạng về sau.
    /// </summary>
    internal sealed class LiveOpsTextRecord
    {
        private const string FormatKey = "format";
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.Ordinal);

        public LiveOpsTextRecord(int format)
        {
            SetInt(FormatKey, format);
        }

        private LiveOpsTextRecord()
        {
        }

        public int Format => GetInt(FormatKey, 0);

        public static bool TryDecode(string text, out LiveOpsTextRecord record)
        {
            record = null;
            if (string.IsNullOrEmpty(text)) return false;
            var decoded = new LiveOpsTextRecord();
            foreach (string line in text.Split('\n'))
            {
                if (line.Length == 0) continue;
                int separator = line.IndexOf('=');
                if (separator <= 0) return false;
                if (!TryUnescape(line.Substring(separator + 1), out string value)) return false;
                decoded._values[line.Substring(0, separator)] = value;
            }
            if (!decoded._values.ContainsKey(FormatKey)) return false;
            record = decoded;
            return true;
        }

        public string Encode()
        {
            var builder = new StringBuilder();
            var keys = new List<string>(_values.Keys);
            keys.Sort(StringComparer.Ordinal);
            foreach (string key in keys)
            {
                builder.Append(key).Append('=').Append(Escape(_values[key])).Append('\n');
            }
            return builder.ToString();
        }

        public void SetString(string key, string value)
        {
            ValidateKey(key);
            _values[key] = value ?? string.Empty;
        }

        public string GetString(string key, string fallback)
        {
            return _values.TryGetValue(key, out string value) ? value : fallback;
        }

        public void SetInt(string key, int value)
        {
            SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public int GetInt(string key, int fallback)
        {
            return _values.TryGetValue(key, out string value) &&
                   int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
        }

        public void SetLong(string key, long value)
        {
            SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }

        public long GetLong(string key, long fallback)
        {
            return _values.TryGetValue(key, out string value) &&
                   long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
                ? parsed
                : fallback;
        }

        public void SetBool(string key, bool value)
        {
            SetString(key, value ? "1" : "0");
        }

        public bool GetBool(string key, bool fallback)
        {
            return _values.TryGetValue(key, out string value) ? value == "1" : fallback;
        }

        /// <summary>Lưu bằng tick UTC để không phải parse chuỗi ngày theo văn hoá của máy.</summary>
        public void SetDateTime(string key, DateTime value)
        {
            SetLong(key, value.Ticks);
        }

        public DateTime GetDateTime(string key, DateTime fallback)
        {
            long ticks = GetLong(key, long.MinValue);
            if (ticks < DateTime.MinValue.Ticks || ticks > DateTime.MaxValue.Ticks) return fallback;
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        // ---------------------------------------------------------------- Kiểu phức hợp dùng chung

        public void SetStringList(string prefix, IReadOnlyList<string> values)
        {
            SetInt(prefix + ".count", values.Count);
            for (int index = 0; index < values.Count; index++)
            {
                SetString(prefix + "." + index.ToString(CultureInfo.InvariantCulture), values[index]);
            }
        }

        public List<string> GetStringList(string prefix)
        {
            int count = GetInt(prefix + ".count", 0);
            var values = new List<string>(count > 0 ? count : 0);
            for (int index = 0; index < count; index++)
            {
                string value = GetString(prefix + "." + index.ToString(CultureInfo.InvariantCulture), string.Empty);
                if (value.Length > 0) values.Add(value);
            }
            return values;
        }

        public void SetBundle(string prefix, LiveOpsRewardBundle bundle)
        {
            bundle = bundle ?? LiveOpsRewardBundle.None;
            SetString(prefix + ".presentation", bundle.PresentationId);
            SetInt(prefix + ".items", bundle.Items.Count);
            for (int index = 0; index < bundle.Items.Count; index++)
            {
                string itemPrefix = prefix + ".item" + index.ToString(CultureInfo.InvariantCulture);
                SetString(itemPrefix + ".id", bundle.Items[index].ItemId);
                SetInt(itemPrefix + ".amount", bundle.Items[index].Amount);
            }
        }

        public LiveOpsRewardBundle GetBundle(string prefix)
        {
            string presentationId = GetString(prefix + ".presentation", string.Empty);
            int count = GetInt(prefix + ".items", 0);
            var items = new List<LiveOpsRewardItem>(count > 0 ? count : 0);
            for (int index = 0; index < count; index++)
            {
                string itemPrefix = prefix + ".item" + index.ToString(CultureInfo.InvariantCulture);
                string itemId = GetString(itemPrefix + ".id", string.Empty);
                int amount = GetInt(itemPrefix + ".amount", 0);
                if (itemId.Length > 0 && amount > 0) items.Add(new LiveOpsRewardItem(itemId, amount));
            }
            var bundle = new LiveOpsRewardBundle(presentationId, items);
            return bundle.IsEmpty ? LiveOpsRewardBundle.None : bundle;
        }

        public void SetInstance(string prefix, LiveEventInstance instance)
        {
            SetString(prefix + ".id", instance.EventId);
            SetString(prefix + ".type", instance.EventType);
            SetDateTime(prefix + ".start", instance.StartUtc);
            SetDateTime(prefix + ".end", instance.EndUtc);
            SetString(prefix + ".config", instance.ConfigKey);
        }

        /// <summary>Null khi dữ liệu thiếu hoặc không còn hợp lệ.</summary>
        public LiveEventInstance GetInstance(string prefix)
        {
            string eventId = GetString(prefix + ".id", string.Empty);
            string eventType = GetString(prefix + ".type", string.Empty);
            DateTime startUtc = GetDateTime(prefix + ".start", DateTime.MinValue);
            DateTime endUtc = GetDateTime(prefix + ".end", DateTime.MinValue);
            if (eventId.Length == 0 || eventType.Length == 0 || endUtc <= startUtc) return null;
            try
            {
                return new LiveEventInstance(eventId, eventType, startUtc, endUtc, GetString(prefix + ".config", string.Empty));
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        // ---------------------------------------------------------------- Nội bộ

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Khoá không được rỗng.", nameof(key));
            if (key.IndexOf('=') >= 0 || key.IndexOf('\n') >= 0 || key.IndexOf('\r') >= 0)
            {
                throw new ArgumentException("Khoá không được chứa '=' hay xuống dòng: " + key, nameof(key));
            }
        }

        private static string Escape(string value)
        {
            if (value.IndexOf('\\') < 0 && value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0) return value;
            var builder = new StringBuilder(value.Length + 8);
            foreach (char character in value)
            {
                switch (character)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    default: builder.Append(character); break;
                }
            }
            return builder.ToString();
        }

        private static bool TryUnescape(string value, out string result)
        {
            if (value.IndexOf('\\') < 0)
            {
                result = value;
                return true;
            }
            var builder = new StringBuilder(value.Length);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character != '\\')
                {
                    builder.Append(character);
                    continue;
                }
                if (index + 1 >= value.Length)
                {
                    result = null;
                    return false;
                }
                char next = value[++index];
                if (next == '\\') builder.Append('\\');
                else if (next == 'n') builder.Append('\n');
                else if (next == 'r') builder.Append('\r');
                else
                {
                    result = null;
                    return false;
                }
            }
            result = builder.ToString();
            return true;
        }
    }
}
