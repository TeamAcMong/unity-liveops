using System;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Một field đổi của một mục, giữ nguyên văn chữ hai phía (giờ hỏng như "2026-10-3" phải hiện đúng cái người dùng gõ,
    /// không bị chuẩn hoá mất dấu vết). Tên field là tên JSON ("startUtc", "configKey"…) hoặc tên thuộc tính loại
    /// ("displayName", "colorSlot", "requiresJoin", "defaultConfigKey", "order") — Editor chọn câu theo tên này.
    /// </summary>
    public sealed class LiveEventCalendarFieldChange
    {
        public LiveEventCalendarFieldChange(string fieldName, string beforeText, string afterText)
        {
            if (string.IsNullOrEmpty(fieldName)) throw new ArgumentException("fieldName không được rỗng.", nameof(fieldName));

            FieldName = fieldName;
            BeforeText = beforeText ?? string.Empty;
            AfterText = afterText ?? string.Empty;
        }

        public string FieldName { get; }
        public string BeforeText { get; }
        public string AfterText { get; }

        public override string ToString() => FieldName + ": " + BeforeText + " → " + AfterText;
    }
}
