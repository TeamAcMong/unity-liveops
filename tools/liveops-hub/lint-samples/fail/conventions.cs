// lint-sample-path: Packages/com.dreamtech.liveops/Editor/Hub/Sections/Calendar/CalendarSample.cs
// Mẫu vi phạm quy ước: lớp không sealed, var không đi với new, field m_, tên viết tắt, so chuỗi/đọc số không culture.
// lint-expect: class-not-sealed
// lint-expect: var-without-new
// lint-expect: field-m-prefix
// lint-expect: abbreviated-name
// lint-expect: abbreviated-name
// lint-expect: string-comparison
// lint-expect: invariant-culture
using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    internal class CalendarSample
    {
        private int m_Count;

        public int Measure(List<string> names, string text)
        {
            var total = names.Count;
            names.ForEach(evt => Console.WriteLine(evt));
            try
            {
                return total + m_Count + int.Parse(text);
            }
            catch (FormatException ex)
            {
                return text.StartsWith("#") ? -1 : ex.HResult;
            }
        }
    }
}
