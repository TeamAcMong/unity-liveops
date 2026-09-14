using System.Collections.Generic;
using System.Globalization;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Bộ dò cú pháp JSON chỉ trong Editor (PD-13): tìm lỗi ĐẦU TIÊN và vị trí dòng/ký tự để hub nói "Dòng 3, ký tự 18: thiếu
    /// dấu phẩy". Parser game vẫn là <c>JsonUtility</c> (không trả vị trí và không cần) — bộ dò này không thay parser, chỉ
    /// giải thích vì sao parser không đọc được; không định vị được thì hub chỉ hiện câu lỗi của parser, không bịa vị trí (S-11).
    /// <para>
    /// Quét lặp bằng ngăn xếp tường minh, không đệ quy: JSON dán vào có thể lồng rất sâu ("[[[[…") và đệ quy sẽ tràn stack của
    /// Editor. Dòng/ký tự đếm từ 1; "\r\n" là một lần xuống dòng; cặp surrogate là một ký tự.
    /// </para>
    /// </summary>
    internal static class LiveOpsJsonSyntaxLocator
    {
        public const string ReasonMissingComma = "missing-comma";
        public const string ReasonUnexpectedEnd = "unexpected-end";
        public const string ReasonUnterminatedString = "unterminated-string";
        public const string ReasonUnexpectedCharacter = "unexpected-character";

        internal sealed class SyntaxError
        {
            internal SyntaxError(int line, int column, string reasonCode, int offset)
            {
                Line = line;
                Column = column;
                ReasonCode = reasonCode;
                Offset = offset;
            }

            public int Line { get; }
            public int Column { get; }

            /// <summary>Một hằng <c>Reason…</c> của <see cref="LiveOpsJsonSyntaxLocator"/>.</summary>
            public string ReasonCode { get; }

            /// <summary>Chỉ số ký tự UTF-16 trong chuỗi gốc — JSON view cuộn/tô tới đúng chỗ mà không đếm lại dòng.</summary>
            public int Offset { get; }
        }

        private enum ScanState
        {
            ExpectValue = 0,
            ExpectValueOrArrayClose = 1,
            ExpectKey = 2,
            ExpectKeyOrObjectClose = 3,
            ExpectColon = 4,
            AfterValue = 5,
        }

        /// <summary>
        /// true + <paramref name="error"/> khi chuỗi không phải JSON hợp lệ; false khi hợp lệ. Không bao giờ ném: null hoặc chuỗi
        /// trắng là "JSON kết thúc giữa chừng" tại vị trí cuối.
        /// </summary>
        public static bool TryFindFirstError(string json, out SyntaxError error)
        {
            string text = json ?? string.Empty;
            int errorOffset;
            string reasonCode;
            if (Scan(text, out errorOffset, out reasonCode))
            {
                error = null;
                return false;
            }
            error = CreateError(text, errorOffset, reasonCode);
            return true;
        }

        /// <summary>"Dòng 3, ký tự 18: thiếu dấu phẩy" — một câu cho Foldout JSON, popover Dán, cổng Xuất.</summary>
        public static string Describe(SyntaxError error)
        {
            if (error == null) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.KitJsonSyntaxPositionFormat, error.Line, error.Column,
                SentenceOf(error.ReasonCode));
        }

        private static string SentenceOf(string reasonCode)
        {
            switch (reasonCode)
            {
                case ReasonMissingComma: return LiveOpsHubStrings.KitJsonSyntaxMissingComma;
                case ReasonUnexpectedEnd: return LiveOpsHubStrings.KitJsonSyntaxUnexpectedEnd;
                case ReasonUnterminatedString: return LiveOpsHubStrings.KitJsonSyntaxUnterminatedString;
                default: return LiveOpsHubStrings.KitJsonSyntaxUnexpectedCharacter;
            }
        }

        /// <returns>true khi hợp lệ.</returns>
        private static bool Scan(string text, out int errorOffset, out string reasonCode)
        {
            var containers = new Stack<char>();
            ScanState state = ScanState.ExpectValue;
            // Vị trí ngay sau giá trị vừa đọc xong: thiếu dấu phẩy được báo ở ĐÂY (chỗ phải chèn dấu phẩy, cùng dòng với giá
            // trị trước) chứ không ở token kế — token kế thường đã sang dòng sau, báo ở đó làm người dùng tìm sai dòng.
            int lastValueEnd = 0;
            int index = 0;
            // BOM đầu chuỗi (file lưu từ Notepad) không phải lỗi người dùng cần sửa tay.
            if (text.Length > 0 && text[0] == '\uFEFF') index = 1;

            while (true)
            {
                index = SkipWhitespace(text, index);
                if (index >= text.Length)
                {
                    if (state == ScanState.AfterValue && containers.Count == 0) return Valid(out errorOffset, out reasonCode);
                    return Fail(text.Length, ReasonUnexpectedEnd, out errorOffset, out reasonCode);
                }
                char character = text[index];
                switch (state)
                {
                    case ScanState.ExpectValue:
                    case ScanState.ExpectValueOrArrayClose:
                        if (character == ']' && state == ScanState.ExpectValueOrArrayClose)
                        {
                            containers.Pop();
                            index++;
                            lastValueEnd = index;
                            state = ScanState.AfterValue;
                            break;
                        }
                        if (character == '{')
                        {
                            containers.Push('{');
                            index++;
                            state = ScanState.ExpectKeyOrObjectClose;
                            break;
                        }
                        if (character == '[')
                        {
                            containers.Push('[');
                            index++;
                            state = ScanState.ExpectValueOrArrayClose;
                            break;
                        }
                        if (!TryScanScalar(text, index, out int scalarEnd, out errorOffset, out reasonCode)) return false;
                        index = scalarEnd;
                        lastValueEnd = index;
                        state = ScanState.AfterValue;
                        break;

                    case ScanState.ExpectKey:
                    case ScanState.ExpectKeyOrObjectClose:
                        if (character == '}' && state == ScanState.ExpectKeyOrObjectClose)
                        {
                            containers.Pop();
                            index++;
                            lastValueEnd = index;
                            state = ScanState.AfterValue;
                            break;
                        }
                        // Dấu phẩy thừa trước '}' cũng rơi vào đây: '}' ở chỗ phải là khoá → ký tự không đúng chỗ.
                        if (character != '"') return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
                        if (!TryScanString(text, index, out int keyEnd, out errorOffset, out reasonCode)) return false;
                        index = keyEnd;
                        state = ScanState.ExpectColon;
                        break;

                    case ScanState.ExpectColon:
                        if (character != ':') return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
                        index++;
                        state = ScanState.ExpectValue;
                        break;

                    default:
                        if (containers.Count == 0)
                        {
                            // Nội dung sau giá trị gốc (vd hai object liền nhau) không phải thiếu dấu phẩy — JSON chỉ có một gốc.
                            return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
                        }
                        char container = containers.Peek();
                        if (character == ',')
                        {
                            index++;
                            state = container == '{' ? ScanState.ExpectKey : ScanState.ExpectValue;
                            break;
                        }
                        if ((character == '}' && container == '{') || (character == ']' && container == '['))
                        {
                            containers.Pop();
                            index++;
                            lastValueEnd = index;
                            state = ScanState.AfterValue;
                            break;
                        }
                        if (IsValueStart(character)) return Fail(lastValueEnd, ReasonMissingComma, out errorOffset, out reasonCode);
                        return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
                }
            }
        }

        private static bool TryScanScalar(string text, int start, out int end, out int errorOffset, out string reasonCode)
        {
            char character = text[start];
            if (character == '"') return TryScanString(text, start, out end, out errorOffset, out reasonCode);
            end = start;
            if (character == '-' || IsDigit(character))
            {
                if (!TryScanNumber(text, start, out end, out errorOffset, out reasonCode)) return false;
            }
            else if (character == 't')
            {
                if (!TryScanLiteral(text, start, "true", out end, out errorOffset, out reasonCode)) return false;
            }
            else if (character == 'f')
            {
                if (!TryScanLiteral(text, start, "false", out end, out errorOffset, out reasonCode)) return false;
            }
            else if (character == 'n')
            {
                if (!TryScanLiteral(text, start, "null", out end, out errorOffset, out reasonCode)) return false;
            }
            else
            {
                return Fail(start, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
            }
            // Số/literal phải dừng ở ranh giới: "01", "truex", "1.2.3" là một token hỏng, không phải hai giá trị thiếu dấu phẩy.
            if (end < text.Length && IsTokenContinuation(text[end])) return Fail(end, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
            return Valid(out errorOffset, out reasonCode);
        }

        private static bool TryScanString(string text, int start, out int end, out int errorOffset, out string reasonCode)
        {
            end = start;
            int index = start + 1;
            while (true)
            {
                if (index >= text.Length) return Fail(start, ReasonUnterminatedString, out errorOffset, out reasonCode);
                char character = text[index];
                if (character == '"')
                {
                    end = index + 1;
                    return Valid(out errorOffset, out reasonCode);
                }
                // Chuỗi JSON không được xuống dòng: gặp xuống dòng gần như luôn là quên đóng nháy — báo ở nháy mở để thấy chuỗi nào.
                if (character == '\n' || character == '\r') return Fail(start, ReasonUnterminatedString, out errorOffset, out reasonCode);
                if (character < ' ') return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
                if (character != '\\')
                {
                    index++;
                    continue;
                }
                index++;
                if (index >= text.Length) return Fail(start, ReasonUnterminatedString, out errorOffset, out reasonCode);
                char escaped = text[index];
                if (escaped == 'u')
                {
                    for (int hexIndex = 1; hexIndex <= 4; hexIndex++)
                    {
                        if (index + hexIndex >= text.Length) return Fail(start, ReasonUnterminatedString, out errorOffset, out reasonCode);
                        if (!IsHexDigit(text[index + hexIndex])) return Fail(index + hexIndex, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
                    }
                    index += 5;
                    continue;
                }
                if (!IsSimpleEscape(escaped)) return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
                index++;
            }
        }

        private static bool TryScanNumber(string text, int start, out int end, out int errorOffset, out string reasonCode)
        {
            int index = start;
            end = start;
            if (text[index] == '-') index++;
            if (!TryScanDigits(text, ref index, true, out errorOffset, out reasonCode)) return false;
            if (index < text.Length && text[index] == '.')
            {
                index++;
                if (!TryScanDigits(text, ref index, false, out errorOffset, out reasonCode)) return false;
            }
            if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
            {
                index++;
                if (index < text.Length && (text[index] == '+' || text[index] == '-')) index++;
                if (!TryScanDigits(text, ref index, false, out errorOffset, out reasonCode)) return false;
            }
            end = index;
            return Valid(out errorOffset, out reasonCode);
        }

        /// <param name="integerPart">Phần nguyên: "0" đứng riêng, không có số 0 dẫn đầu.</param>
        private static bool TryScanDigits(string text, ref int index, bool integerPart, out int errorOffset, out string reasonCode)
        {
            if (index >= text.Length) return Fail(text.Length, ReasonUnexpectedEnd, out errorOffset, out reasonCode);
            if (!IsDigit(text[index])) return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
            if (integerPart && text[index] == '0')
            {
                index++;
                return Valid(out errorOffset, out reasonCode);
            }
            while (index < text.Length && IsDigit(text[index])) index++;
            return Valid(out errorOffset, out reasonCode);
        }

        private static bool TryScanLiteral(string text, int start, string literal, out int end, out int errorOffset, out string reasonCode)
        {
            end = start;
            for (int offset = 0; offset < literal.Length; offset++)
            {
                int index = start + offset;
                if (index >= text.Length) return Fail(text.Length, ReasonUnexpectedEnd, out errorOffset, out reasonCode);
                if (text[index] != literal[offset]) return Fail(index, ReasonUnexpectedCharacter, out errorOffset, out reasonCode);
            }
            end = start + literal.Length;
            return Valid(out errorOffset, out reasonCode);
        }

        private static SyntaxError CreateError(string text, int offset, string reasonCode)
        {
            int line = 1;
            int column = 1;
            int limit = offset < text.Length ? offset : text.Length;
            for (int index = 0; index < limit; index++)
            {
                char character = text[index];
                if (character == '\n')
                {
                    line++;
                    column = 1;
                }
                else if (character == '\r')
                {
                    // "\r\n" đếm một lần (ở '\n'); '\r' đứng riêng (Mac cũ) vẫn là xuống dòng.
                    if (index + 1 < text.Length && text[index + 1] == '\n') continue;
                    line++;
                    column = 1;
                }
                else if (!char.IsLowSurrogate(character))
                {
                    column++;
                }
            }
            return new SyntaxError(line, column, reasonCode, offset);
        }

        private static int SkipWhitespace(string text, int index)
        {
            while (index < text.Length)
            {
                char character = text[index];
                if (character != ' ' && character != '\t' && character != '\n' && character != '\r') break;
                index++;
            }
            return index;
        }

        private static bool IsValueStart(char character)
        {
            return character == '"' || character == '{' || character == '[' || character == '-' || IsDigit(character)
                || character == 't' || character == 'f' || character == 'n';
        }

        private static bool IsTokenContinuation(char character)
        {
            return IsDigit(character) || character == '.' || (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z')
                || character == '_';
        }

        private static bool IsDigit(char character) => character >= '0' && character <= '9';

        private static bool IsSimpleEscape(char character)
        {
            return character == '"' || character == '\\' || character == '/' || character == 'b' || character == 'f'
                || character == 'n' || character == 'r' || character == 't';
        }

        private static bool IsHexDigit(char character)
        {
            return IsDigit(character) || (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F');
        }

        private static bool Valid(out int errorOffset, out string reasonCode)
        {
            errorOffset = -1;
            reasonCode = string.Empty;
            return true;
        }

        private static bool Fail(int offset, string reason, out int errorOffset, out string reasonCode)
        {
            errorOffset = offset;
            reasonCode = reason;
            return false;
        }
    }
}
