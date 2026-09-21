using System.Text;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class JsonSyntaxLocatorTests
    {
        private const string Format2Like =
            "{\n" +
            "  \"version\": 2,\n" +
            "  \"recurring\": [\n" +
            "    {\n" +
            "      \"type\": \"weekly-pass\",\n" +
            "      \"anchorUtc\": \"2026-01-05T00:00:00Z\",\n" +
            "      \"idPrefix\": \"pass-\",\n" +
            "      \"periodHours\": 168,\n" +
            "      \"activeHours\": 168\n" +
            "    }\n" +
            "  ],\n" +
            "  \"events\": [\n" +
            "    { \"id\": \"hunt-0914\", \"note\": \"\\\"quoted\\\" \\\\ \\u00e9 \\/\", \"ratio\": -1.5e+3, \"flag\": true, \"other\": false, \"none\": null },\n" +
            "    { \"empty\": {}, \"list\": [] }\n" +
            "  ]\n" +
            "}";

        private static LiveOpsJsonSyntaxLocator.SyntaxError FindError(string json)
        {
            Assert.IsTrue(LiveOpsJsonSyntaxLocator.TryFindFirstError(json, out LiveOpsJsonSyntaxLocator.SyntaxError error), "phải báo lỗi");
            Assert.IsNotNull(error);
            return error;
        }

        private static void AssertError(string json, int line, int column, string reasonCode)
        {
            LiveOpsJsonSyntaxLocator.SyntaxError error = FindError(json);
            Assert.AreEqual(reasonCode, error.ReasonCode, "mã lỗi");
            Assert.AreEqual(line, error.Line, "dòng");
            Assert.AreEqual(column, error.Column, "ký tự");
        }

        [Test]
        public void ValidFormat2Like_NoError()
        {
            Assert.IsFalse(LiveOpsJsonSyntaxLocator.TryFindFirstError(Format2Like, out LiveOpsJsonSyntaxLocator.SyntaxError error));
            Assert.IsNull(error);
        }

        [Test]
        public void JsonSyntaxLocator_MissingComma_ReportsLineAndColumn()
        {
            // Lỗi báo ở chỗ phải chèn dấu phẩy — ngay sau "p" trên dòng 3 — không ở khoá kế tiếp trên dòng 4.
            const string json =
                "{\n" +
                "  \"type\": \"weekly-pass\",\n" +
                "  \"idPrefix\": \"p\"\n" +
                "  \"periodHours\": 168\n" +
                "}";

            LiveOpsJsonSyntaxLocator.SyntaxError error = FindError(json);

            Assert.AreEqual(LiveOpsJsonSyntaxLocator.ReasonMissingComma, error.ReasonCode);
            Assert.AreEqual(3, error.Line);
            Assert.AreEqual(18, error.Column);
            Assert.AreEqual("Dòng 3, ký tự 18: thiếu dấu phẩy", LiveOpsJsonSyntaxLocator.Describe(error));
        }

        [Test]
        public void JsonSyntaxLocator_UnexpectedEnd()
        {
            AssertError("{\n  \"events\": [\n", 3, 1, LiveOpsJsonSyntaxLocator.ReasonUnexpectedEnd);
        }

        [Test]
        public void EmptyOrNull_UnexpectedEndAtStart_NoThrow()
        {
            AssertError("", 1, 1, LiveOpsJsonSyntaxLocator.ReasonUnexpectedEnd);
            AssertError(null, 1, 1, LiveOpsJsonSyntaxLocator.ReasonUnexpectedEnd);
            AssertError("   ", 1, 4, LiveOpsJsonSyntaxLocator.ReasonUnexpectedEnd);
        }

        [Test]
        public void UnterminatedString_ReportsOpeningQuote()
        {
            AssertError("{\"a\": \"abc}", 1, 7, LiveOpsJsonSyntaxLocator.ReasonUnterminatedString);
            AssertError("[\"abc\n\"]", 1, 2, LiveOpsJsonSyntaxLocator.ReasonUnterminatedString);
        }

        [Test]
        public void MissingCommaInArray_ReportsAfterPreviousValue()
        {
            AssertError("[1 2]", 1, 3, LiveOpsJsonSyntaxLocator.ReasonMissingComma);
            AssertError("[{} {}]", 1, 4, LiveOpsJsonSyntaxLocator.ReasonMissingComma);
        }

        [Test]
        public void TrailingComma_UnexpectedCharacterAtClose()
        {
            AssertError("{\"a\": 1,}", 1, 9, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("[1,]", 1, 4, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
        }

        [Test]
        public void MissingColonOrMismatchedClose_UnexpectedCharacter()
        {
            AssertError("{\"a\" 1}", 1, 6, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("[1}", 1, 3, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
        }

        [Test]
        public void CrLfLineEndings_CountedOnce()
        {
            AssertError("{\r\n  \"a\": 1\r\n  \"b\": 2\r\n}", 2, 9, LiveOpsJsonSyntaxLocator.ReasonMissingComma);
        }

        [Test]
        public void BadTokens_UnexpectedCharacter()
        {
            AssertError("[01]", 1, 3, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("[tru]", 1, 5, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("[truex]", 1, 6, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("[1.]", 1, 4, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("[\"\\x\"]", 1, 4, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("[\"\\u12G4\"]", 1, 7, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
        }

        [Test]
        public void ContentAfterRoot_UnexpectedCharacter()
        {
            AssertError("{} {}", 1, 4, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
        }

        /// <summary>
        /// Dấu phẩy NỐI HAI GỐC cũng là lỗi với người gọi mặc định: cổng Xuất và popover Dán đang dò CẢ một tài liệu, mà
        /// một tài liệu chỉ có một gốc. Ô JSON một-luật của màn Luật lặp là ngoại lệ phải TỰ KHAI (xem test dưới) — để nó
        /// thành luật chung thì mọi file nhiều gốc mất dòng/ký tự ở cổng Xuất và người dùng chỉ còn câu trần của parser.
        /// </summary>
        [Test]
        public void TopLevelComma_IsErrorForDocumentCallers()
        {
            AssertError("{},{}", 1, 3, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
            AssertError("{},", 1, 3, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
        }

        /// <summary>
        /// Người gọi khai <c>allowMultipleRoots</c> (mẩu sắp được bọc vào một mảng) mới được nối nhiều gốc bằng dấu phẩy —
        /// và chỉ ĐƯỢC NGẦN ĐÓ: dấu phẩy cụt vẫn là JSON kết thúc giữa chừng, không phải "hợp lệ vì đã khai".
        /// </summary>
        [Test]
        public void TopLevelComma_AllowedOnlyWhenCallerAsksForMultipleRoots()
        {
            LiveOpsJsonSyntaxLocator.SyntaxError error;
            Assert.IsFalse(LiveOpsJsonSyntaxLocator.TryFindFirstError("{},{}", true, out error),
                "hai gốc cách nhau bằng dấu phẩy là cú pháp hợp lệ với người gọi đã khai");
            Assert.IsTrue(LiveOpsJsonSyntaxLocator.TryFindFirstError("{},", true, out error), "dấu phẩy cụt vẫn hỏng");
            Assert.AreEqual(LiveOpsJsonSyntaxLocator.ReasonUnexpectedEnd, error.ReasonCode);
        }

        [Test]
        public void SurrogatePair_CountsAsOneCharacter()
        {
            string emoji = char.ConvertFromUtf32(0x1F600);
            AssertError("[\"" + emoji + "\" x]", 1, 6, LiveOpsJsonSyntaxLocator.ReasonUnexpectedCharacter);
        }

        [Test]
        public void LeadingByteOrderMark_ReportedAtLineOneColumnOne()
        {
            // JsonUtility (parser game) ném với BOM đầu chuỗi ở cả hai bản Unity — bộ dò phải báo lỗi, không được nói hợp lệ.
            string byteOrderMark = ((char)0xFEFF).ToString();
            AssertError(byteOrderMark + "{\"a\": 1}", 1, 1, LiveOpsJsonSyntaxLocator.ReasonByteOrderMark);
            Assert.AreEqual(0, FindError(byteOrderMark + "{\"a\": 1}").Offset);
        }

        [Test]
        public void DeepNesting_NoStackOverflow()
        {
            const int depth = 100000;
            var builder = new StringBuilder(depth * 2);
            builder.Append('[', depth);
            string unclosed = builder.ToString();
            builder.Append(']', depth);

            Assert.IsFalse(LiveOpsJsonSyntaxLocator.TryFindFirstError(builder.ToString(), out _));
            Assert.AreEqual(LiveOpsJsonSyntaxLocator.ReasonUnexpectedEnd, FindError(unclosed).ReasonCode);
        }

        [Test]
        public void Offset_PointsIntoOriginalText()
        {
            LiveOpsJsonSyntaxLocator.SyntaxError error = FindError("[1 2]");
            Assert.AreEqual(2, error.Offset);
        }

        [Test]
        public void Describe_EveryReason_HasSentence()
        {
            Assert.AreEqual("Dòng 1, ký tự 1: JSON kết thúc giữa chừng", LiveOpsJsonSyntaxLocator.Describe(FindError("")));
            Assert.AreEqual("Dòng 1, ký tự 2: chuỗi chưa đóng dấu nháy", LiveOpsJsonSyntaxLocator.Describe(FindError("[\"a")));
            Assert.AreEqual("Dòng 1, ký tự 2: ký tự không đúng chỗ", LiveOpsJsonSyntaxLocator.Describe(FindError("[,]")));
            Assert.AreEqual("Dòng 1, ký tự 1: đầu JSON có ký tự BOM (U+FEFF) mà parser của game không đọc được — lưu lại dạng UTF-8 không BOM",
                LiveOpsJsonSyntaxLocator.Describe(FindError(((char)0xFEFF).ToString() + "{}")));
            Assert.AreEqual(string.Empty, LiveOpsJsonSyntaxLocator.Describe(null));
        }
    }
}
