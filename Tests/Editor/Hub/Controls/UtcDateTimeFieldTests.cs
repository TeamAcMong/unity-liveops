using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Ô giờ UTC trong panel thật (ChangeEvent chỉ dispatch khi element có panel): đọc chặt yyyy-MM-dd + HH:mm, chuỗi hỏng giữ nguyên
    /// và báo lỗi thay vì tự sửa, gợi ý phủ ô không chặn click.
    /// </summary>
    [TestFixture]
    [Category(LiveOpsHubTestCategories.UI)]
    public sealed class UtcDateTimeFieldTests
    {
        private static readonly TimeSpan DeviceOffset = TimeSpan.FromHours(7);

        private ControlsTestPanel _panel;

        [TearDown]
        public void TearDown()
        {
            _panel?.Dispose();
            _panel = null;
        }

        [UnityTest]
        public IEnumerator UtcField_ValidInput_SetsValueAndDeviceLine()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            yield return ControlsTestPanel.WaitForLayout(field);
            field.SetRawTextWithoutNotify("2026-09-15", "12:00");
            List<(DateTime PreviousValue, DateTime NewValue)> changes = RecordChanges(field);
            List<string> rawCommits = RecordRawCommits(field);

            field.DateInput.value = "2026-09-16";
            yield return null;

            Assert.AreEqual(new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc), field.value);
            Assert.AreEqual(DateTimeKind.Utc, field.value.Kind, "giá trị của ô luôn là giờ UTC");
            Assert.IsFalse(field.HasParseError);
            Assert.AreEqual(1, changes.Count, "một lần ghi ô ngày = đúng một ChangeEvent<DateTime>");
            Assert.AreEqual(new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc), changes[0].PreviousValue);
            Assert.AreEqual(field.value, changes[0].NewValue);
            Assert.AreEqual(0, rawCommits.Count, "chuỗi đọc được không đi đường ghi chuỗi thô");

            Assert.AreEqual("19:00 16/9 giờ máy", field.DeviceTimeLabel.text, "dòng phụ phải là giờ máy +7 và luôn có chữ \"giờ máy\"");
            Assert.AreEqual(DisplayStyle.Flex, field.DeviceTimeLabel.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, field.ErrorLabel.resolvedStyle.display, "chữ đọc được thì không có dòng lỗi");
            Assert.AreEqual(DisplayStyle.None, field.ErrorIcon.resolvedStyle.display);

            field.TimeInput.value = "00:00";
            field.DateInput.value = "2026-09-18";
            yield return null;
            Assert.AreEqual("07:00 18/9 giờ máy", field.DeviceTimeLabel.text);
            Assert.AreEqual(3, changes.Count);

            Assert.AreEqual(76f, field.DateInput.layout.width, 0.5f, "ô ngày 76px ([SD1 §3.1])");
            Assert.AreEqual(44f, field.TimeInput.layout.width, 0.5f, "ô giờ 44px");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UtcField_BrokenDate_KeepsRawShowsError()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            yield return ControlsTestPanel.WaitForLayout(field);
            field.SetRawTextWithoutNotify("2026-09-16", "12:00");
            DateTime lastReadable = field.value;
            List<(DateTime PreviousValue, DateTime NewValue)> changes = RecordChanges(field);
            List<string> rawCommits = RecordRawCommits(field);
            Color normalBorder = InputOf(field.DateInput).resolvedStyle.borderTopColor;

            field.DateInput.value = "2026-10-3";
            yield return null;
            yield return null;

            Assert.AreEqual("2026-10-3", field.RawDateText, "chuỗi không đọc được phải giữ nguyên văn — không nuốt, không tự thêm số 0");
            Assert.AreEqual("2026-10-3", field.DateInput.value);
            Assert.IsTrue(field.HasParseError);
            Assert.AreEqual(lastReadable, field.value, "giá trị giữ lần đọc được gần nhất");
            Assert.AreEqual(0, changes.Count, "chuỗi hỏng không được thành ChangeEvent<DateTime>");
            CollectionAssert.AreEqual(new[] { "2026-10-3|12:00" }, rawCommits, "phiên lịch phải nhận chuỗi thô để ghi nguyên văn vào asset");

            Assert.IsTrue(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));
            Assert.IsFalse(field.TimeInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError), "chỉ viền đúng ô hỏng");
            Assert.AreNotEqual(normalBorder, InputOf(field.DateInput).resolvedStyle.borderTopColor, "viền ô hỏng phải đổi sang blocked-fill");
            Assert.AreEqual(DisplayStyle.Flex, field.ErrorLabel.resolvedStyle.display);
            StringAssert.Contains("\"2026-10-3\"", field.ErrorLabel.text, "dòng lỗi phải nêu chuỗi người dùng gõ");
            StringAssert.Contains("2026-10-03 (yyyy-MM-dd)", field.ErrorLabel.text, "dòng lỗi phải nêu cách viết đúng");
            Assert.IsTrue(field.ErrorLabel.ClassListContains(LiveOpsHubClassNames.TextBlocked));
            Assert.AreEqual(DisplayStyle.Flex, field.ErrorIcon.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, field.DeviceTimeLabel.resolvedStyle.display, "không đọc được thì không có giờ máy để hiện");

            // Sửa về đúng ngày cũ: giá trị không đổi nhưng asset đang giữ chuỗi thô → vẫn phải báo đúng một lần.
            field.DateInput.value = "2026-09-16";
            yield return null;
            Assert.IsFalse(field.HasParseError);
            Assert.AreEqual(1, changes.Count, "sửa chuỗi hỏng về giá trị cũ vẫn phải báo để phiên ghi lại chuỗi chuẩn");
            Assert.AreEqual(DisplayStyle.None, field.ErrorLabel.resolvedStyle.display);
            Assert.IsFalse(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));

            // Chuỗi hỏng nạp từ asset: hiện lỗi ngay, không bắn sự kiện nào.
            field.SetRawTextWithoutNotify("2026-10-3", "00:00");
            Assert.IsTrue(field.HasParseError);
            Assert.AreEqual("2026-10-3", field.RawDateText);
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(1, rawCommits.Count);

            // Giờ hỏng: viền ô giờ, câu nói về ô giờ.
            field.SetRawTextWithoutNotify("2026-10-03", "7:0");
            Assert.IsTrue(field.TimeInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));
            Assert.IsFalse(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));
            StringAssert.Contains("HH:mm", field.ErrorLabel.text);

            // Câu của người gọi (phát hiện utc-time-format) thắng câu mặc định; bỏ câu thì câu mặc định quay lại.
            field.SetErrorText("Ô ngày cần dạng 2026-10-03 (yyyy-MM-dd).");
            Assert.AreEqual("Ô ngày cần dạng 2026-10-03 (yyyy-MM-dd).", field.ErrorLabel.text);
            field.SetErrorText(null);
            StringAssert.Contains("\"7:0\"", field.ErrorLabel.text);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UtcField_PlaceholderPickingModeIgnore()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            yield return ControlsTestPanel.WaitForLayout(field);

            foreach (LiveOpsPlaceholder placeholder in new[] { field.DatePlaceholder, field.TimePlaceholder })
            {
                Assert.AreEqual(PickingMode.Ignore, placeholder.pickingMode,
                    "gợi ý bắt click thì chặn focus vào ô và chặn cả IME tiếng Việt ([FD §2.11])");
                Assert.IsFalse(placeholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "ô trống phải hiện gợi ý");
                Assert.AreEqual(DisplayStyle.Flex, placeholder.resolvedStyle.display);
                Assert.IsTrue(ControlsTestPanel.HasLayout(placeholder), "gợi ý phải có kích thước để nhìn thấy");
                Assert.AreSame(field.panel, placeholder.panel);

                VisualElement picked = field.panel.Pick(placeholder.worldBound.center);
                Assert.IsNotNull(picked, "điểm giữa gợi ý phải trúng một element");
                Assert.AreNotSame(placeholder, picked, "Pick trúng chính gợi ý — click sẽ không tới ô nhập");
                TextField owner = placeholder.Field;
                Assert.IsTrue(owner == picked || owner.Contains(picked), "click lên gợi ý phải rơi vào ô nhập bên dưới, trúng " + picked);
            }
            Assert.AreEqual(LiveOpsHubStrings.UtcFieldDatePlaceholder, field.DatePlaceholder.text);
            Assert.AreEqual(LiveOpsHubStrings.UtcFieldTimePlaceholder, field.TimePlaceholder.text);

            field.SetValueWithoutNotify(new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));
            yield return null;
            Assert.IsTrue(field.DatePlaceholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "ô có giá trị thì ẩn gợi ý");
            Assert.IsTrue(field.TimePlaceholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden));
            Assert.AreEqual("2026-01-05", field.RawDateText);
            Assert.AreEqual("00:00", field.RawTimeText);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void UtcField_TryParseParts_StrictFormats()
        {
            Assert.IsTrue(LiveOpsUtcDateTimeField.TryParseParts("2026-10-03", "07:30", out DateTime parsed));
            Assert.AreEqual(new DateTime(2026, 10, 3, 7, 30, 0, DateTimeKind.Utc), parsed);
            Assert.IsTrue(LiveOpsUtcDateTimeField.TryParseParts("2026-10-03", "07:30:15", out parsed));
            Assert.AreEqual(15, parsed.Second);
            Assert.IsFalse(LiveOpsUtcDateTimeField.TryParseParts("2026-10-3", "00:00", out parsed), "thiếu số 0 là lỗi ở ô (sửa là việc của đề xuất)");
            Assert.IsFalse(LiveOpsUtcDateTimeField.TryParseParts("3/10/2026", "00:00", out parsed));
            Assert.IsFalse(LiveOpsUtcDateTimeField.TryParseParts("2026-10-03", "24:00", out parsed));
            Assert.IsFalse(LiveOpsUtcDateTimeField.TryParseParts("2026-10-03", string.Empty, out parsed));
            Assert.IsFalse(LiveOpsUtcDateTimeField.TryParseParts(string.Empty, "00:00", out parsed));
            StringAssert.Contains("yyyy-MM-dd", LiveOpsUtcDateTimeField.DescribeParseError("3/10/2026", "00:00"));
            Assert.AreEqual(LiveOpsHubStrings.UtcFieldDateEmpty, LiveOpsUtcDateTimeField.DescribeParseError(string.Empty, "00:00"));
            Assert.AreEqual(LiveOpsHubStrings.UtcFieldTimeEmpty, LiveOpsUtcDateTimeField.DescribeParseError("2026-10-03", string.Empty));
        }

        private LiveOpsUtcDateTimeField CreateField(bool lightSkin)
        {
            _panel = ControlsTestPanel.Open();
            VisualElement root = _panel.CreateRoot(lightSkin);
            LiveOpsUtcDateTimeField field = new LiveOpsUtcDateTimeField();
            field.SetDeviceOffset(DeviceOffset);
            root.Add(field);
            return field;
        }

        private static VisualElement InputOf(TextField textField)
        {
            return textField.Q(className: TextField.inputUssClassName);
        }

        private static List<(DateTime PreviousValue, DateTime NewValue)> RecordChanges(LiveOpsUtcDateTimeField field)
        {
            List<(DateTime PreviousValue, DateTime NewValue)> changes = new List<(DateTime PreviousValue, DateTime NewValue)>();
            // Sự kiện pool bị trả lại sau dispatch — chép giá trị ra thay vì giữ tham chiếu tới sự kiện.
            field.RegisterValueChangedCallback(changeEvent => changes.Add((changeEvent.previousValue, changeEvent.newValue)));
            return changes;
        }

        private static List<string> RecordRawCommits(LiveOpsUtcDateTimeField field)
        {
            List<string> commits = new List<string>();
            field.RawTextCommitted += (dateText, timeText) => commits.Add(dateText + "|" + timeText);
            return commits;
        }
    }
}
