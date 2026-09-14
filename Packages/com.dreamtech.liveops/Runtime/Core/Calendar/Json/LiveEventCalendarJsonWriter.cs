using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Bộ ghi JSON lịch viết tay, tất định từng byte (mục 5.2, PD-6). Vì sao không dùng JsonUtility: nó không bỏ được
    /// field của asset (entryKey, dấu, cảnh báo), không cố định thụt lề / định dạng số / ngày, và sha phải giống hệt nhau
    /// giữa 2022.3 và 6000.6 trên mọi culture — dấu đã đăng và "Khớp dấu" dựa vào đúng chuỗi byte này.
    /// </summary>
    public static class LiveEventCalendarJsonWriter
    {
        private const char LineFeed = '\n';
        private const string IndentUnit = "  ";

        // Tên key JSON — đúng thứ parser 0.1.0/0.2.0 đọc; đổi một chữ là game bỏ field im lặng (JsonUtility bỏ key lạ).
        internal const string VersionKey = "version";
        internal const string RecurringKey = "recurring";
        internal const string EventsKey = "events";
        internal const string TypeKey = "type";
        internal const string AnchorUtcKey = "anchorUtc";
        internal const string IdPrefixKey = "idPrefix";
        internal const string PeriodHoursKey = "periodHours";
        internal const string ActiveHoursKey = "activeHours";
        internal const string ConfigKeyKey = "configKey";
        internal const string IdKey = "id";
        internal const string StartUtcKey = "startUtc";
        internal const string EndUtcKey = "endUtc";

        private const int RootIndentLevel = 0;
        private const int ArrayElementIndentLevel = 2;

        public static LiveEventCalendarJsonText Write(LiveEventCalendarDocument document, LiveEventCalendarJsonFormat format)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (format != LiveEventCalendarJsonFormat.Version1 && format != LiveEventCalendarJsonFormat.Version2)
            {
                throw new ArgumentOutOfRangeException(nameof(format), "Định dạng JSON lịch chỉ có 1 hoặc 2.");
            }

            // Luật 8 (V-6): thứ tự events do MỘT hàm core quyết — cùng hàm bộ biên dịch nháp và validator gọi, để "mục thứ N"
            // và đợt nào thắng khi trùng id khớp đúng cái game nhận. Luật lặp giữ thứ tự asset (luật 7, V-12).
            LiveEventCalendarDocument ordered = LiveEventCalendarExportOrder.Apply(document);
            var writer = new LineWriter();

            writer.AppendRootLine(RootIndentLevel, "{", string.Empty);
            if (format == LiveEventCalendarJsonFormat.Version2)
            {
                writer.AppendRootLine(1, Quote(VersionKey) + ": " + FormatInteger((int)format) + ",", VersionKey);
                WriteRecurringArray(writer, ordered);
            }
            WriteEventsArray(writer, ordered);
            writer.AppendRootLine(RootIndentLevel, "}", string.Empty);

            return new LiveEventCalendarJsonText(format, writer.BuildText(), writer.Lines);
        }

        /// <summary>
        /// Bản không khoảng trắng ngoài chuỗi, chỉ để XEM (PD-12): Copy, Lưu file và sha luôn dùng bản đã định dạng — hai sha
        /// cho một nháp sẽ làm hộp "Đánh dấu đã đăng" đối chiếu sai.
        /// </summary>
        public static string WriteSingleLine(LiveEventCalendarJsonText formatted)
        {
            if (formatted == null) throw new ArgumentNullException(nameof(formatted));

            string text = formatted.Text;
            var builder = new StringBuilder(text.Length);
            bool insideString = false;
            bool previousWasEscape = false;
            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                if (insideString)
                {
                    builder.Append(character);
                    if (previousWasEscape) previousWasEscape = false;
                    else if (character == '\\') previousWasEscape = true;
                    else if (character == '"') insideString = false;
                    continue;
                }

                // Ngoài chuỗi bộ ghi chỉ sinh dấu cách và LF; khoảng trắng trong giá trị (tên, ghi chú) phải giữ nguyên.
                if (character == ' ' || character == LineFeed || character == '\r' || character == '\t') continue;
                if (character == '"') insideString = true;
                builder.Append(character);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Một đợt dưới dạng object JSON đứng riêng (thụt từ cột 0) cho "Copy đợt (JSON)" — cùng luật viết với mảng
        /// <c>events</c> (configKey hiệu lực, giờ chuẩn hoá hoặc nguyên văn), để dán vào JSON remote là đúng cái game đọc.
        /// </summary>
        public static string WriteFixedEventObject(LiveEventCalendarDocument document, FixedLiveEventEntry entry)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (entry == null) throw new ArgumentNullException(nameof(entry));

            var writer = new LineWriter();
            AppendFixedEventObject(writer, document, entry, RootIndentLevel, isLastElement: true);
            return writer.BuildText();
        }

        /// <summary>Một luật lặp dưới dạng object JSON đứng riêng cho Foldout "JSON của luật này" — cùng luật viết với <c>recurring</c>.</summary>
        public static string WriteRecurringRuleObject(LiveEventCalendarDocument document, RecurringLiveEventRule rule)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (rule == null) throw new ArgumentNullException(nameof(rule));

            var writer = new LineWriter();
            AppendRecurringRuleObject(writer, document, rule, RootIndentLevel, isLastElement: true);
            return writer.BuildText();
        }

        private static void WriteRecurringArray(LineWriter writer, LiveEventCalendarDocument document)
        {
            IReadOnlyList<RecurringLiveEventRule> rules = document.RecurringRules;
            // Định dạng 2 luôn ghi recurring kể cả rỗng: parser 0.2.0 nhận ra định dạng 2 khi thiếu version bằng chính mảng này.
            if (rules.Count == 0)
            {
                writer.AppendRootLine(1, Quote(RecurringKey) + ": [],", RecurringKey);
                return;
            }

            writer.AppendRootLine(1, Quote(RecurringKey) + ": [", RecurringKey);
            for (int index = 0; index < rules.Count; index++)
            {
                AppendRecurringRuleObject(writer, document, rules[index], ArrayElementIndentLevel, index == rules.Count - 1);
            }
            writer.AppendRootLine(1, "],", string.Empty);
        }

        private static void WriteEventsArray(LineWriter writer, LiveEventCalendarDocument document)
        {
            IReadOnlyList<FixedLiveEventEntry> entries = document.FixedEvents;
            if (entries.Count == 0)
            {
                writer.AppendRootLine(1, Quote(EventsKey) + ": []", EventsKey);
                return;
            }

            writer.AppendRootLine(1, Quote(EventsKey) + ": [", EventsKey);
            for (int index = 0; index < entries.Count; index++)
            {
                AppendFixedEventObject(writer, document, entries[index], ArrayElementIndentLevel, index == entries.Count - 1);
            }
            writer.AppendRootLine(1, "]", string.Empty);
        }

        private static void AppendRecurringRuleObject(LineWriter writer, LiveEventCalendarDocument document, RecurringLiveEventRule rule,
            int indentLevel, bool isLastElement)
        {
            const LiveEventCalendarItemKind kind = LiveEventCalendarItemKind.RecurringRule;
            string itemKey = rule.EventType;
            int fieldIndentLevel = indentLevel + 1;

            writer.AppendItemLine(indentLevel, "{", kind, itemKey, string.Empty);
            writer.AppendItemLine(fieldIndentLevel, StringField(TypeKey, rule.EventType, isLastField: false), kind, itemKey, TypeKey);
            writer.AppendItemLine(fieldIndentLevel, StringField(AnchorUtcKey, TimeText(rule.AnchorUtcText), isLastField: false), kind, itemKey, AnchorUtcKey);
            // Luật 6: ghi tiền tố hiệu lực — diff/sha không được phụ thuộc mặc định của runtime, dù game 0.2.0 mặc định cùng giá trị.
            writer.AppendItemLine(fieldIndentLevel, StringField(IdPrefixKey, rule.EffectiveIdPrefix, isLastField: false), kind, itemKey, IdPrefixKey);
            writer.AppendItemLine(fieldIndentLevel, IntegerField(PeriodHoursKey, rule.PeriodHours), kind, itemKey, PeriodHoursKey);
            writer.AppendItemLine(fieldIndentLevel, IntegerField(ActiveHoursKey, rule.ActiveHours), kind, itemKey, ActiveHoursKey);
            writer.AppendItemLine(fieldIndentLevel, StringField(ConfigKeyKey, document.EffectiveConfigKeyOf(rule), isLastField: true), kind, itemKey, ConfigKeyKey);
            writer.AppendItemLine(indentLevel, isLastElement ? "}" : "},", kind, itemKey, string.Empty);
        }

        private static void AppendFixedEventObject(LineWriter writer, LiveEventCalendarDocument document, FixedLiveEventEntry entry,
            int indentLevel, bool isLastElement)
        {
            const LiveEventCalendarItemKind kind = LiveEventCalendarItemKind.FixedEvent;
            string itemKey = entry.EntryKey;
            int fieldIndentLevel = indentLevel + 1;

            writer.AppendItemLine(indentLevel, "{", kind, itemKey, string.Empty);
            writer.AppendItemLine(fieldIndentLevel, StringField(IdKey, entry.EventId, isLastField: false), kind, itemKey, IdKey);
            writer.AppendItemLine(fieldIndentLevel, StringField(TypeKey, entry.EventType, isLastField: false), kind, itemKey, TypeKey);
            writer.AppendItemLine(fieldIndentLevel, StringField(StartUtcKey, TimeText(entry.StartUtcText), isLastField: false), kind, itemKey, StartUtcKey);
            writer.AppendItemLine(fieldIndentLevel, StringField(EndUtcKey, TimeText(entry.EndUtcText), isLastField: false), kind, itemKey, EndUtcKey);
            // Luật 5: configKey luôn có mặt — thiếu key thì game đọc "" và không ai thấy đợt đó đang kế thừa mặc định nào.
            writer.AppendItemLine(fieldIndentLevel, StringField(ConfigKeyKey, document.EffectiveConfigKeyOf(entry), isLastField: true), kind, itemKey, ConfigKeyKey);
            writer.AppendItemLine(indentLevel, isLastElement ? "}" : "},", kind, itemKey, string.Empty);
        }

        /// <summary>
        /// Luật 4: giờ đọc được → dạng chuẩn <c>yyyy-MM-ddTHH:mm:ssZ</c>; không đọc được → nguyên văn, vì đó đúng là cái game
        /// sẽ đọc và bỏ — hub phải cho thấy "2026-10-3" trong JSON thay vì một giờ đã đoán.
        /// </summary>
        private static string TimeText(string utcText)
        {
            return LiveEventUtcText.TryParse(utcText, out DateTime utc) ? LiveEventUtcText.Format(utc) : utcText;
        }

        private static string StringField(string key, string value, bool isLastField)
        {
            var builder = new StringBuilder(key.Length + value.Length + 8);
            AppendQuoted(builder, key);
            builder.Append(": ");
            AppendQuoted(builder, value);
            if (!isLastField) builder.Append(',');
            return builder.ToString();
        }

        // periodHours / activeHours không bao giờ là field cuối của object (configKey luôn đứng sau), nên luôn có dấu phẩy.
        private static string IntegerField(string key, int value)
        {
            return Quote(key) + ": " + FormatInteger(value) + ",";
        }

        // Luật 3: InvariantCulture — StringBuilder.Append(int) và int.ToString() đi theo culture của thread (dấu âm có thể
        // không phải '-' ở vài culture), làm lệch byte giữa hai máy.
        private static string FormatInteger(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string Quote(string value)
        {
            var builder = new StringBuilder(value.Length + 2);
            AppendQuoted(builder, value);
            return builder.ToString();
        }

        /// <summary>
        /// Luật 2: escape đúng <c>"</c>, <c>\</c> và ký tự điều khiển &lt; 0x20 (<c>\n</c> <c>\r</c> <c>\t</c>, còn lại
        /// <c>\u00XX</c> hex thường). Ký tự không ASCII giữ nguyên để byte UTF-8 là đúng thứ JsonUtility đọc lại và người
        /// đọc JSON thấy tiếng Việt thay vì <c>\u1ec7</c>.
        /// </summary>
        private static void AppendQuoted(StringBuilder builder, string value)
        {
            const string lowercaseHexDigits = "0123456789abcdef";

            builder.Append('"');
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 0x20)
                        {
                            builder.Append("\\u00");
                            builder.Append(lowercaseHexDigits[character >> 4]);
                            builder.Append(lowercaseHexDigits[character & 0x0F]);
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }
            builder.Append('"');
        }

        /// <summary>Gom dòng + siêu dữ liệu dòng; nối bằng LF, không LF cuối (luật 1).</summary>
        private sealed class LineWriter
        {
            public readonly List<LiveEventCalendarJsonLine> Lines = new List<LiveEventCalendarJsonLine>();

            public void AppendRootLine(int indentLevel, string content, string fieldName)
            {
                Lines.Add(new LiveEventCalendarJsonLine(Lines.Count + 1, Indent(indentLevel) + content, null, string.Empty, fieldName));
            }

            public void AppendItemLine(int indentLevel, string content, LiveEventCalendarItemKind kind, string itemKey, string fieldName)
            {
                Lines.Add(new LiveEventCalendarJsonLine(Lines.Count + 1, Indent(indentLevel) + content, kind, itemKey, fieldName));
            }

            public string BuildText()
            {
                var builder = new StringBuilder();
                for (int index = 0; index < Lines.Count; index++)
                {
                    if (index > 0) builder.Append(LineFeed);
                    builder.Append(Lines[index].Text);
                }
                return builder.ToString();
            }

            private static string Indent(int indentLevel)
            {
                if (indentLevel == 0) return string.Empty;
                var builder = new StringBuilder(indentLevel * IndentUnit.Length);
                for (int level = 0; level < indentLevel; level++) builder.Append(IndentUnit);
                return builder.ToString();
            }
        }
    }
}
