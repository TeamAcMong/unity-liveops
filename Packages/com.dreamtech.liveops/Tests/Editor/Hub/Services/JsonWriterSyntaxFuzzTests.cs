using System;
using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Bộ ghi JSON lịch (viết tay, không qua JsonUtility) luôn ra JSON đúng cú pháp theo bộ dò của hub, trên 200 tài liệu ngẫu
    /// nhiên seed cố định — kể cả chuỗi có dấu nháy, gạch chéo ngược, tab, xuống dòng, ký tự điều khiển, tiếng Việt, emoji. Vì
    /// sao dùng bộ dò của hub làm trọng tài: màn Xuất JSON và "Dán JSON" dùng chính nó để báo "Dòng N, ký tự M"; bộ ghi sinh
    /// một byte sai cú pháp thì hub tự báo JSON của mình hỏng và parser game bỏ cả lịch. Phạm vi đó được test tự đếm và đòi
    /// có mặt trong chính JSON đã ghi (không chỉ trong tài liệu): tên hiển thị của loại không được ghi ra, nên ký tự chỉ nằm ở
    /// đó thì không bao giờ qua bộ ghi.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class JsonWriterSyntaxFuzzTests
    {
        private static readonly LiveEventCalendarJsonFormat[] Formats = { LiveEventCalendarJsonFormat.Version1, LiveEventCalendarJsonFormat.Version2 };

        [Test]
        public void OutputPassesSyntaxCheck_ForRandomDocuments()
        {
            int jsonWithEscapedTabCount = 0;
            int jsonWithSurrogatePairCount = 0;
            int jsonWithEscapedQuoteOrBackslashCount = 0;
            int jsonWithControlEscapeCount = 0;
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
                {
                    LiveEventCalendarJsonFormat format = Formats[formatIndex];
                    string label = LiveOpsRandomDocuments.Label(documentIndex) + " · định dạng " + ((int)format).ToString(CultureInfo.InvariantCulture);
                    LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(document, format);

                    AssertNoSyntaxError(label, json.Text, document);
                    if (ContainsEscape(json.Text, 't')) jsonWithEscapedTabCount++;
                    if (ContainsSurrogatePair(json.Text)) jsonWithSurrogatePairCount++;
                    if (ContainsEscape(json.Text, '"') || ContainsEscape(json.Text, '\\')) jsonWithEscapedQuoteOrBackslashCount++;
                    if (ContainsEscape(json.Text, 'u')) jsonWithControlEscapeCount++;
                    // Bản một dòng chỉ để xem (PD-12) nhưng vẫn hiện trong ô JSON — bỏ khoảng trắng sai chỗ cũng làm hỏng cú pháp.
                    AssertNoSyntaxError(label + " (một dòng)", LiveEventCalendarJsonWriter.WriteSingleLine(json), document);
                }
            }

            // Chặn bộ sinh bị "làm dễ" về sau: fuzz vẫn xanh nhưng không còn JSON nào mang ký tự khó thì không bắt được gì.
            Assert.Greater(jsonWithEscapedTabCount, 0, "Phải có JSON đã ghi chứa tab (\\t) — tab chỉ ở tên hiển thị thì không qua bộ ghi.");
            Assert.Greater(jsonWithSurrogatePairCount, 0, "Phải có JSON đã ghi chứa emoji (cặp surrogate).");
            Assert.Greater(jsonWithEscapedQuoteOrBackslashCount, 0, "Phải có JSON đã ghi chứa dấu nháy hoặc gạch chéo ngược đã escape.");
            Assert.Greater(jsonWithControlEscapeCount, 0, "Phải có JSON đã ghi chứa ký tự điều khiển dạng \\u00XX.");
        }

        /// <summary>
        /// Có chuỗi thoát <c>\&lt;chữ&gt;</c> thật (không phải gạch chéo ngược đã escape đứng trước chữ đó) — quét từng cặp thoát
        /// thay vì IndexOf, vì <c>\\t</c> (gạch chéo + chữ t) cũng chứa chuỗi con <c>\t</c>.
        /// </summary>
        private static bool ContainsEscape(string text, char escapeLetter)
        {
            for (int index = 0; index + 1 < text.Length; index++)
            {
                if (text[index] != '\\') continue;
                if (text[index + 1] == escapeLetter) return true;
                index++;
            }
            return false;
        }

        private static bool ContainsSurrogatePair(string text)
        {
            for (int index = 0; index + 1 < text.Length; index++)
            {
                if (char.IsSurrogatePair(text[index], text[index + 1])) return true;
            }
            return false;
        }

        private static void AssertNoSyntaxError(string label, string text, LiveEventCalendarDocument document)
        {
            bool hasError = LiveOpsJsonSyntaxLocator.TryFindFirstError(text, out LiveOpsJsonSyntaxLocator.SyntaxError error);
            Assert.IsFalse(hasError, hasError
                ? label + ": " + LiveOpsJsonSyntaxLocator.Describe(error) + "\n" + text + "\n" + LiveOpsRandomDocuments.Describe(document)
                : string.Empty);
        }
    }
}
