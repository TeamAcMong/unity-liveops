// lint-sample-path: Packages/com.dreamtech.liveops/Runtime/Core/Calendar/Document/LiveEventSample.cs
// Mẫu đạt: core thuần, sealed, var chỉ đi với new, so chuỗi có StringComparison, số có InvariantCulture.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps
{
    public sealed class LiveEventSample
    {
        private readonly List<string> _identifiers = new List<string>();

        public bool Contains(string identifier)
        {
            foreach (string candidate in _identifiers)
            {
                if (string.Equals(candidate, identifier, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        public static int ReadHours(string text)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int hours = int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
            return seen.Count + hours;
        }

        public static IEnumerable<int> Doubled(IEnumerable<int> values)
        {
            List<int> result = new List<int>();
            foreach (int value in values) result.Add(value * 2);
            return result;
        }
    }
}
