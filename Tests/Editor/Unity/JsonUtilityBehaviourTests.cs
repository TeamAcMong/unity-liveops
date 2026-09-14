using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DreamTech.LiveOps.Unity.Tests
{
    /// <summary>
    /// Khoá hành vi THẬT của <see cref="JsonUtility.FromJson{T}(string)"/> mà <see cref="JsonLiveEventCalendarParser"/> dựa
    /// vào (SP-11, R-21). Mỗi tên test ghi đúng kết quả đã đo — đo 14/9/2026 bằng probe riêng, giống hệt nhau ở 2022.3.62f2
    /// và 6000.6.0f1 (log <c>plan/w1/G-UNITY-jsonutility-probe-*.txt</c>). Đổi bản Unity mà hành vi đổi thì test này đỏ
    /// TRƯỚC khi parser lặng lẽ đọc sai lịch trên máy người chơi.
    /// </summary>
    [TestFixture]
    public sealed class JsonUtilityBehaviourTests
    {
#pragma warning disable 0649 // Field do JsonUtility gán.
        [Serializable]
        private sealed class RuleProbe
        {
            public string type;
            public string idPrefix;
            public int periodHours;
        }

        [Serializable]
        private sealed class ObjectFieldProbe
        {
            public int version;
            public RuleProbe recurring;
        }

        [Serializable]
        private sealed class EntryProbe
        {
            public string id;
        }

        [Serializable]
        private sealed class ArrayFieldProbe
        {
            public int version;
            public RuleProbe[] recurring;
            public EntryProbe[] events;
            public string name;
            public int count;
        }
#pragma warning restore 0649

        private readonly List<string> _receivedLogs = new List<string>();

        [SetUp]
        public void SetUp()
        {
            _receivedLogs.Clear();
            Application.logMessageReceived += OnLogMessageReceived;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _receivedLogs.Add(type + ": " + condition);
        }

        private void AssertNothingLogged()
        {
            // "Im lặng" là một phần hành vi cần khoá: parser không có gì để báo Problem khi JsonUtility tự ép kiểu, nên nếu một
            // bản Unity bắt đầu log Warning thì console của game sẽ đầy log mà Problems vẫn trống.
            Assert.IsEmpty(_receivedLogs, string.Join("\n", _receivedLogs));
        }

        [Test]
        public void EmptyOrNullText_ReturnsNullNoThrow()
        {
            // Probe SP-11 cũ báo NullReferenceException — thật ra do probe đọc field của kết quả null. Parser phải kiểm null.
            Assert.IsNull(JsonUtility.FromJson<ArrayFieldProbe>(string.Empty));
            Assert.IsNull(JsonUtility.FromJson<ArrayFieldProbe>(null));
            AssertNothingLogged();
        }

        [Test]
        public void WhitespaceText_ThrowsArgumentException()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() => JsonUtility.FromJson<ArrayFieldProbe>("   "));
            StringAssert.Contains("The document is empty", exception.Message);
        }

        [Test]
        public void RecurringMissingEmptyOrNull_IsNonNullInstance()
        {
            // Field kiểu object lồng không bao giờ null: vắng mặt, {} và null cho cùng một instance mặc định — không phân biệt được
            // "không có luật" bằng == null. Dấu "không có luật" đã chốt: periodHours == 0 và mọi chuỗi con null. (Parser thật khai
            // recurring là MẢNG nên không vướng bẫy này — xem MissingArray_IsNull / NullArrayLiteral_IsEmptyArray.)
            foreach (string json in new[] { "{}", "{\"recurring\":{}}", "{\"recurring\":null}" })
            {
                ObjectFieldProbe result = JsonUtility.FromJson<ObjectFieldProbe>(json);
                Assert.IsNotNull(result, json);
                Assert.IsNotNull(result.recurring, json);
                Assert.AreEqual(0, result.recurring.periodHours, json);
                Assert.IsNull(result.recurring.idPrefix, json);
                Assert.IsNull(result.recurring.type, json);
            }
            AssertNothingLogged();
        }

        [Test]
        public void NumberInStringField_CoercedSilently()
        {
            Assert.AreEqual("5", JsonUtility.FromJson<ArrayFieldProbe>("{\"name\":5}").name);
            Assert.AreEqual("true", JsonUtility.FromJson<ArrayFieldProbe>("{\"name\":true}").name);
            AssertNothingLogged();
        }

        [Test]
        public void NumberInStringField_FractionalWrittenWithSixDecimals()
        {
            // "id": 2.5 thành "2.500000", không phải "2.5" — id đợt đọc được nhưng khác chữ người đăng đã gõ.
            Assert.AreEqual("2.500000", JsonUtility.FromJson<ArrayFieldProbe>("{\"name\":2.5}").name);
            AssertNothingLogged();
        }

        [Test]
        public void MissingArray_IsNull()
        {
            ArrayFieldProbe result = JsonUtility.FromJson<ArrayFieldProbe>("{}");
            Assert.IsNull(result.events);
            Assert.IsNull(result.recurring);
            AssertNothingLogged();
        }

        [Test]
        public void NullArrayLiteral_IsEmptyArray()
        {
            // Khác mảng vắng mặt: "recurring": null cho mảng rỗng — parser coi đây là JSON CÓ ghi key recurring (định dạng 2).
            ArrayFieldProbe result = JsonUtility.FromJson<ArrayFieldProbe>("{\"events\":null,\"recurring\":null}");
            Assert.IsNotNull(result.events);
            Assert.AreEqual(0, result.events.Length);
            Assert.IsNotNull(result.recurring);
            Assert.AreEqual(0, result.recurring.Length);
            AssertNothingLogged();
        }

        [Test]
        public void NullArrayElement_IsInstanceWithNullFields()
        {
            ArrayFieldProbe result = JsonUtility.FromJson<ArrayFieldProbe>("{\"events\":[null]}");
            Assert.AreEqual(1, result.events.Length);
            Assert.IsNotNull(result.events[0]);
            Assert.IsNull(result.events[0].id);
            AssertNothingLogged();
        }

        [Test]
        public void MissingInt_IsZero()
        {
            ArrayFieldProbe result = JsonUtility.FromJson<ArrayFieldProbe>("{\"events\":[]}");
            Assert.AreEqual(0, result.version);
            Assert.AreEqual(0, result.count);
            AssertNothingLogged();
        }

        [Test]
        public void StringInIntField_NumericTextParsedSilently()
        {
            Assert.AreEqual(24, JsonUtility.FromJson<ArrayFieldProbe>("{\"count\":\"24\"}").count);
            AssertNothingLogged();
        }

        [Test]
        public void StringInIntField_NonNumericTextBecomesZero()
        {
            // "periodHours": "abc" thành 0 im lặng — bộ biên dịch bỏ luật vì "chu kỳ phải > 0 giờ", không bao giờ tới exception.
            Assert.AreEqual(0, JsonUtility.FromJson<ArrayFieldProbe>("{\"count\":\"abc\"}").count);
            AssertNothingLogged();
        }

        [Test]
        public void IntegerOutOfRange_BecomesMinusOne()
        {
            // Không cắt modulo 2^32 mà trả -1: chu kỳ khổng lồ trong JSON thành -1, bộ biên dịch bỏ luật "chu kỳ phải > 0 giờ".
            Assert.AreEqual(-1, JsonUtility.FromJson<ArrayFieldProbe>("{\"count\":300000000000}").count);
            AssertNothingLogged();
        }

        [Test]
        public void ObjectWhereArrayExpected_IsNullNoThrow()
        {
            Assert.IsNull(JsonUtility.FromJson<ArrayFieldProbe>("{\"events\":{}}").events);
            Assert.IsNull(JsonUtility.FromJson<ArrayFieldProbe>("{\"events\":{\"id\":\"a\"}}").events);
            Assert.IsNull(JsonUtility.FromJson<ArrayFieldProbe>("{\"events\":\"x\"}").events);
            AssertNothingLogged();
        }

        [Test]
        public void Utf8Bom_ThrowsArgumentException()
        {
            // File lưu bằng trình soạn thảo thêm BOM rồi dán vào remote config: JsonUtility không bỏ BOM → parser báo "JSON lịch
            // event hỏng" (bộ ghi của hub không bao giờ ghi BOM, mục 5.2).
            ArgumentException exception = Assert.Throws<ArgumentException>(
                () => JsonUtility.FromJson<ArrayFieldProbe>("﻿{\"version\":2,\"events\":[]}"));
            StringAssert.Contains("Invalid value", exception.Message);
        }

        [Test]
        public void FractionalVersion_TruncatedToInt()
        {
            Assert.AreEqual(2, JsonUtility.FromJson<ArrayFieldProbe>("{\"version\":2.5,\"events\":[]}").version);
            Assert.AreEqual(2, JsonUtility.FromJson<ArrayFieldProbe>("{\"version\":2.0,\"events\":[]}").version);
            AssertNothingLogged();
        }

        [Test]
        public void RootNotObject_ThrowsArgumentException()
        {
            ArgumentException arrayException = Assert.Throws<ArgumentException>(() => JsonUtility.FromJson<ArrayFieldProbe>("[]"));
            StringAssert.Contains("JSON must represent an object type", arrayException.Message);
            ArgumentException nullLiteralException = Assert.Throws<ArgumentException>(() => JsonUtility.FromJson<ArrayFieldProbe>("null"));
            StringAssert.Contains("JSON must represent an object type", nullLiteralException.Message);
        }
    }
}
