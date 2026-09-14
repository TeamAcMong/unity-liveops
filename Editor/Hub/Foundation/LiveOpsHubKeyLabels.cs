using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nhãn phím đọc từ profile phím THẬT của người dùng ("⌘Z", "⇧⌘Z") thay cho chuỗi ghi cứng — người dùng đổi phím
    /// trong Edit → Shortcuts thì toast/tooltip của hub vẫn đúng. Luật ([API §6.2], S-8):
    /// <c>GetShortcutBinding</c> ném <see cref="ArgumentException"/> với id không tồn tại → trả ""; id có mà không gán phím
    /// (Frame Selected, Soft Delete trên macOS) cũng trả "" — không bao giờ in "⌘" cụt; font thiếu ký hiệu phím thì đổi
    /// sang chữ ("Cmd K").
    /// </summary>
    internal static class LiveOpsHubKeyLabels
    {
        private const string UndoShortcutId = "Main Menu/Edit/Undo";
        private const string RedoShortcutId = "Main Menu/Edit/Redo";

        private const char CommandGlyph = '⌘';
        private const char ShiftGlyph = '⇧';
        private const char OptionGlyph = '⌥';
        private const char BackspaceGlyph = '⌫';

        private static readonly Dictionary<char, bool> GlyphAvailability = new Dictionary<char, bool>();
        private static Font _defaultEditorFont;
        private static bool _defaultEditorFontLoaded;

        /// <summary>Nhãn phím của shortcut; "" khi id không có hoặc không gán phím.</summary>
        public static string For(string shortcutId)
        {
            if (string.IsNullOrEmpty(shortcutId)) return string.Empty;
            string label;
            try
            {
                label = ShortcutManager.instance.GetShortcutBinding(shortcutId).ToString();
            }
            catch (ArgumentException)
            {
                // Id không tồn tại (gõ sai, hoặc shortcut của bản Unity khác) — không có phím để nêu.
                return string.Empty;
            }
            if (string.IsNullOrEmpty(label)) return string.Empty;
            return ReplaceMissingGlyphs(label);
        }

        /// <summary>"(⌘Z)" hoặc "" — để câu kiểu "Hoàn tác (⌘Z)" không còn cặp ngoặc rỗng khi không có phím.</summary>
        public static string WithParentheses(string shortcutId)
        {
            string label = For(shortcutId);
            return label.Length == 0 ? string.Empty : "(" + label + ")";
        }

        public static string Undo => For(UndoShortcutId);

        public static string Redo => For(RedoShortcutId);

        /// <summary>
        /// true khi font Label mặc định của Editor có đủ mọi ký tự trong <paramref name="glyphs"/>. Đọc file Inter-Regular.ttf
        /// (nguồn của Inter-Regular SDF) vì bản SDF không nạp được theo đường dẫn; không nạp được font thì coi là có —
        /// [API §12.3] đã đo đủ glyph ở cả hai bản, đổi sang chữ khi không chắc chỉ làm nhãn xấu đi.
        /// </summary>
        public static bool EditorFontHas(string glyphs)
        {
            if (string.IsNullOrEmpty(glyphs)) return true;
            foreach (char glyph in glyphs)
            {
                if (char.IsWhiteSpace(glyph)) continue;
                if (!HasGlyph(glyph)) return false;
            }
            return true;
        }

        internal static string ReplaceMissingGlyphs(string label)
        {
            string result = label;
            result = ReplaceGlyph(result, ShiftGlyph, LiveOpsHubStrings.KeyLabelShift);
            result = ReplaceGlyph(result, OptionGlyph, LiveOpsHubStrings.KeyLabelOption);
            result = ReplaceGlyph(result, CommandGlyph, LiveOpsHubStrings.KeyLabelCommand);
            result = ReplaceGlyph(result, BackspaceGlyph, LiveOpsHubStrings.KeyLabelBackspace);
            return result;
        }

        private static string ReplaceGlyph(string label, char glyph, string word)
        {
            if (label.IndexOf(glyph.ToString(), StringComparison.Ordinal) < 0 || HasGlyph(glyph)) return label;
            // "⇧⌘Z" → "Shift Cmd Z": mỗi ký hiệu thành một chữ cách bằng khoảng trắng.
            return label.Replace(glyph.ToString(), word + " ");
        }

        private static bool HasGlyph(char glyph)
        {
            if (GlyphAvailability.TryGetValue(glyph, out bool available)) return available;
            Font font = DefaultEditorFont();
            available = font == null || font.HasCharacter(glyph);
            GlyphAvailability[glyph] = available;
            return available;
        }

        private static Font DefaultEditorFont()
        {
            if (!_defaultEditorFontLoaded)
            {
                _defaultEditorFontLoaded = true;
                _defaultEditorFont = EditorGUIUtility.Load(LiveOpsHubPaths.DefaultEditorFontResource) as Font;
            }
            return _defaultEditorFont;
        }
    }
}
