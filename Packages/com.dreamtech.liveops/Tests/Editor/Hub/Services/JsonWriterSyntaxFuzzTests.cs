using System.Globalization;
using DreamTech.LiveOps.Tests;
using NUnit.Framework;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Bộ ghi JSON lịch (viết tay, không qua JsonUtility) luôn ra JSON đúng cú pháp theo bộ dò của hub, trên 200 tài liệu ngẫu
    /// nhiên seed cố định — kể cả chuỗi có dấu nháy, gạch chéo ngược, tab, xuống dòng, ký tự điều khiển, tiếng Việt, emoji. Vì
    /// sao dùng bộ dò của hub làm trọng tài: màn Xuất JSON và "Dán JSON" dùng chính nó để báo "Dòng N, ký tự M"; bộ ghi sinh
    /// một byte sai cú pháp thì hub tự báo JSON của mình hỏng và parser game bỏ cả lịch.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.Logic)]
    public sealed class JsonWriterSyntaxFuzzTests
    {
        private static readonly LiveEventCalendarJsonFormat[] Formats = { LiveEventCalendarJsonFormat.Version1, LiveEventCalendarJsonFormat.Version2 };

        [Test]
        public void OutputPassesSyntaxCheck_ForRandomDocuments()
        {
            for (int documentIndex = 0; documentIndex < LiveOpsRandomDocuments.DocumentCount; documentIndex++)
            {
                LiveEventCalendarDocument document = LiveOpsRandomDocuments.Create(documentIndex);
                for (int formatIndex = 0; formatIndex < Formats.Length; formatIndex++)
                {
                    LiveEventCalendarJsonFormat format = Formats[formatIndex];
                    string label = LiveOpsRandomDocuments.Label(documentIndex) + " · định dạng " + ((int)format).ToString(CultureInfo.InvariantCulture);
                    LiveEventCalendarJsonText json = LiveEventCalendarJsonWriter.Write(document, format);

                    AssertNoSyntaxError(label, json.Text, document);
                    // Bản một dòng chỉ để xem (PD-12) nhưng vẫn hiện trong ô JSON — bỏ khoảng trắng sai chỗ cũng làm hỏng cú pháp.
                    AssertNoSyntaxError(label + " (một dòng)", LiveEventCalendarJsonWriter.WriteSingleLine(json), document);
                }
            }
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
