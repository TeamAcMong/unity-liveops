using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
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

        /// <summary>
        /// Bề rộng ô ngày sau W9-19: 88px = 78px vùng nội dung (chữ 75px ở 2022.3 + 1,5px dư mỗi bên) + 8px đệm "padding 0 4"
        /// mà [SD1 §3.1] pin + 2px viền của chính TextInput. Hai px viền ấy là phần phiếu W9-19 chưa trừ khi đề nghị dải 80–84.
        /// </summary>
        private const float DateInputWidth = 88f;

        /// <summary>
        /// Ngày dài nhất ô phải chứa được. Mọi ngày của hub viết dạng <c>yyyy-MM-dd</c> bằng font mono nên 10 ký tự nào cũng
        /// rộng như nhau; chọn đúng chuỗi mà lượt W8-UX3 đo được 75px trên 2022.3 để con số của test so được với con số ấy.
        /// </summary>
        private const string LongestDateText = "2026-09-10";

        /// <summary>
        /// Khe tối thiểu mỗi bên giữa chữ và mép vùng nội dung của ô. Vì sao phải có một con số: bản trước chỉ kiểm "đã cắt
        /// chưa", mà ô 76px chứa chữ 75px thì chưa cắt — dư 0,5px mỗi bên, và một đổi metric font (bản Unity khác, DPI khác,
        /// cỡ chữ Editor khác) là cắt lại mà không test nào đỏ (W9-19).
        /// </summary>
        private const float RequiredTextSlackPerSide = 1.5f;

        /// <summary>
        /// Sai số của PHÉP ĐO, tách bạch với ngưỡng thiết kế ở trên: <c>MeasureTextSize</c> và lượt vẽ thật làm tròn lệch nhau
        /// vài phần mười px ở cả hai bản Unity (cùng con số 1,5px mà <c>CalendarInspectorUxFixTests</c> dùng cho mọi phép đo
        /// chữ). Vì sao phải cộng vào: theo số của gói, 2022.3 đạt ngưỡng BẰNG ĐÚNG (chữ 75px, vùng nội dung 78px = 88 − 8 đệm
        /// − 2 viền, tức dư đúng 1,5px mỗi bên) nên assert không dung sai sẽ đỏ vì một phần mười pixel làm tròn — thành ca chập
        /// chờn tiếp theo, đúng chủng loại W9-16. NGƯỠNG là <see cref="RequiredTextSlackPerSide"/> × 2 = 3,0px; con số này CHỈ
        /// bù sai số đo và KHÔNG phải một phần của khe hở.
        /// </summary>
        private const float TextMeasureTolerance = 1.5f;

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
            List<string> commits = RecordCommits(field);

            field.DateInput.value = "2026-09-16";
            yield return null;

            Assert.AreEqual(new DateTime(2026, 9, 16, 12, 0, 0, DateTimeKind.Utc), field.value);
            Assert.AreEqual(DateTimeKind.Utc, field.value.Kind, "giá trị của ô luôn là giờ UTC");
            Assert.IsFalse(field.HasParseError);
            Assert.AreEqual(1, changes.Count, "một lần ghi ô ngày = đúng một ChangeEvent<DateTime>");
            Assert.AreEqual(new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc), changes[0].PreviousValue);
            Assert.AreEqual(field.value, changes[0].NewValue);
            CollectionAssert.AreEqual(new[] { "2026-09-16|12:00" }, commits,
                "chốt được một giờ đọc được cũng đi đúng đường chốt đó — nơi nghe chỉ đăng ký một chỗ (UX-04)");

            Assert.AreEqual("19:00 16/9 giờ máy", field.DeviceTimeLabel.text, "dòng phụ phải là giờ máy +7 và luôn có chữ \"giờ máy\"");
            Assert.AreEqual(DisplayStyle.Flex, field.DeviceTimeLabel.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, field.ErrorLabel.resolvedStyle.display, "chữ đọc được thì không có dòng lỗi");
            Assert.AreEqual(DisplayStyle.None, field.ErrorIcon.resolvedStyle.display);

            field.TimeInput.value = "00:00";
            field.DateInput.value = "2026-09-18";
            yield return null;
            Assert.AreEqual("07:00 18/9 giờ máy", field.DeviceTimeLabel.text);
            Assert.AreEqual(3, changes.Count);
            CollectionAssert.AreEqual(new[] { "2026-09-16|12:00", "2026-09-16|00:00", "2026-09-18|00:00" }, commits,
                "ba lần chốt chữ = ba lần báo, không lần nào lặng lẽ");

            Assert.AreEqual(DateInputWidth, field.DateInput.layout.width, 0.5f, "ô ngày 88px (W9-19: chữ + đệm 4+4 của [SD1 §3.1] + 2px viền TextInput)");
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
            List<string> commits = RecordCommits(field);
            Color normalBorder = InputOf(field.DateInput).resolvedStyle.borderTopColor;

            field.DateInput.value = "2026-10-3";
            yield return null;
            yield return null;

            Assert.AreEqual("2026-10-3", field.RawDateText, "chuỗi không đọc được phải giữ nguyên văn — không nuốt, không tự thêm số 0");
            Assert.AreEqual("2026-10-3", field.DateInput.value);
            Assert.IsTrue(field.HasParseError);
            Assert.AreEqual(lastReadable, field.value, "giá trị giữ lần đọc được gần nhất");
            Assert.AreEqual(0, changes.Count, "chuỗi hỏng không được thành ChangeEvent<DateTime>");
            CollectionAssert.AreEqual(new[] { "2026-10-3|12:00" }, commits, "phiên lịch phải nhận chuỗi thô để ghi nguyên văn vào asset");

            Assert.IsTrue(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));
            Assert.IsFalse(field.TimeInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError), "chỉ viền đúng ô hỏng");
            Assert.AreNotEqual(normalBorder, InputOf(field.DateInput).resolvedStyle.borderTopColor, "viền ô hỏng phải đổi sang blocked-fill");
            Assert.AreEqual(DisplayStyle.Flex, field.ErrorLabel.resolvedStyle.display);
            StringAssert.Contains("\"<noparse>2026-10-3</noparse>\"", field.ErrorLabel.text, "dòng lỗi phải nêu chuỗi người dùng gõ, bọc noparse");
            StringAssert.Contains("2026-10-03 (yyyy-MM-dd)", field.ErrorLabel.text, "dòng lỗi phải nêu cách viết đúng");
            Assert.IsTrue(field.ErrorLabel.ClassListContains(LiveOpsHubClassNames.TextBlocked));
            Assert.AreEqual(DisplayStyle.Flex, field.ErrorIcon.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, field.DeviceTimeLabel.resolvedStyle.display, "không đọc được thì không có giờ máy để hiện");

            // Sửa về đúng ngày cũ: giá trị không đổi nhưng asset đang giữ chuỗi thô → vẫn phải báo đúng một lần.
            field.DateInput.value = "2026-09-16";
            yield return null;
            Assert.IsFalse(field.HasParseError);
            Assert.AreEqual(1, changes.Count, "sửa chuỗi hỏng về giá trị cũ vẫn phải báo để phiên ghi lại chuỗi chuẩn");
            CollectionAssert.AreEqual(new[] { "2026-10-3|12:00", "2026-09-16|12:00" }, commits,
                "lần sửa về chuỗi chuẩn cũng là một lần chốt");
            Assert.AreEqual(DisplayStyle.None, field.ErrorLabel.resolvedStyle.display);
            Assert.IsFalse(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));

            // Chuỗi hỏng nạp từ asset: hiện lỗi ngay, không bắn sự kiện nào.
            field.SetRawTextWithoutNotify("2026-10-3", "00:00");
            Assert.IsTrue(field.HasParseError);
            Assert.AreEqual("2026-10-3", field.RawDateText);
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(2, commits.Count, "nạp chuỗi từ asset không phải người dùng chốt — không thêm lần báo nào");

            // Giờ hỏng: viền ô giờ, câu nói về ô giờ.
            field.SetRawTextWithoutNotify("2026-10-03", "7:0");
            Assert.IsTrue(field.TimeInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));
            Assert.IsFalse(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));
            StringAssert.Contains("HH:mm", field.ErrorLabel.text);

            // Hai ô cùng hỏng: viền cả hai và câu nêu cả hai — không để lỗi giờ chỉ lộ ra sau khi sửa xong ngày.
            field.SetRawTextWithoutNotify("2026-10-3", "7:0");
            Assert.IsTrue(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError), "ô ngày hỏng phải có viền");
            Assert.IsTrue(field.TimeInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError), "ô giờ cũng hỏng phải có viền");
            StringAssert.Contains("\"<noparse>2026-10-3</noparse>\"", field.ErrorLabel.text);
            StringAssert.Contains("\"<noparse>7:0</noparse>\"", field.ErrorLabel.text, "câu lỗi phải nêu cả ô giờ");
            field.SetRawTextWithoutNotify("2026-10-03", "7:0");

            // Câu của người gọi (phát hiện utc-time-format) thắng câu mặc định; bỏ câu thì câu mặc định quay lại.
            field.SetErrorText("Ô ngày cần dạng 2026-10-03 (yyyy-MM-dd).");
            Assert.AreEqual("Ô ngày cần dạng 2026-10-03 (yyyy-MM-dd).", field.ErrorLabel.text);
            field.SetErrorText(null);
            StringAssert.Contains("\"<noparse>7:0</noparse>\"", field.ErrorLabel.text);

            // Chuỗi gõ có thẻ rich text: dòng lỗi hiện nguyên văn — đo bề rộng như chữ thường không parse (Label mặc định bật rich text).
            const string taggedDate = "2026-<b>10</b>-3";
            field.SetRawTextWithoutNotify(taggedDate, "00:00");
            StringAssert.Contains("\"<noparse>" + taggedDate + "</noparse>\"", field.ErrorLabel.text);
            Label literal = new Label(field.ErrorLabel.text.Replace("<noparse>", string.Empty).Replace("</noparse>", string.Empty)) { enableRichText = false };
            Label rendered = new Label(field.ErrorLabel.text);
            foreach (Label probe in new[] { literal, rendered })
            {
                probe.AddToClassList(LiveOpsHubClassNames.UtcFieldError);
                probe.style.alignSelf = Align.FlexStart;
                field.parent.Add(probe);
            }
            yield return ControlsTestPanel.WaitForLayout(literal, rendered);
            Assert.AreEqual(literal.layout.width, rendered.layout.width, 1f, "thẻ <b> trong chuỗi gõ không được bị hiểu thành chữ đậm hay bị nuốt");
            StringAssert.Contains("<noparse>a</</noparse><noparse>noparse>b</noparse>", LiveOpsUtcDateTimeField.DescribeParseError("a</noparse>b", "00:00"),
                "chuỗi gõ chứa sẵn </noparse> không được đóng khối noparse sớm");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UtcField_TypingIntoEmptyPart_HidesPlaceholderBeforeCommit()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            yield return ControlsTestPanel.WaitForLayout(field);
            Assert.IsFalse(field.DatePlaceholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "ô trống phải hiện gợi ý");

            // Đúng đường của bàn phím: KeyboardTextEditorEventHandler gọi ITextEdition.UpdateText mỗi phím; ô isDelayed chưa đổi value.
            TypeText(field.DateInput, "2026");
            Assert.AreEqual(string.Empty, field.DateInput.value, "ô isDelayed chưa ghi khi đang gõ — nếu đã ghi thì test không còn đo đúng lỗi");
            Assert.IsTrue(field.DatePlaceholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden),
                "gõ vào ô trống phải ẩn gợi ý ngay, không đợi Enter/rời ô — gợi ý đè lên chữ đang gõ");
            yield return null;
            Assert.AreEqual(DisplayStyle.None, field.DatePlaceholder.resolvedStyle.display);
            Assert.IsFalse(field.TimePlaceholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "ô giờ chưa gõ vẫn hiện gợi ý");

            TypeText(field.DateInput, string.Empty);
            Assert.IsFalse(field.DatePlaceholder.ClassListContains(LiveOpsHubClassNames.PlaceholderHidden), "xoá hết chữ đang gõ thì gợi ý hiện lại");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UtcField_TabIntoEmptyTime_WaitsUntilFocusLeavesField()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            TextField outside = new TextField();
            field.parent.Add(outside);
            yield return ControlsTestPanel.WaitForLayout(field, outside);
            List<(DateTime PreviousValue, DateTime NewValue)> changes = RecordChanges(field);
            List<string> commits = RecordCommits(field);

            yield return MoveFocus(field.DateInput);
            TypeText(field.DateInput, "2026-09-16");
            yield return MoveFocus(field.TimeInput);

            Assert.AreEqual("2026-09-16", field.RawDateText, "rời ô ngày phải chốt chữ của ô ngày");
            Assert.AreEqual(0, commits.Count, "Tab sang ô giờ còn trống không được ghi nửa cặp thành chuỗi thô vào asset");
            Assert.AreEqual(0, changes.Count);
            Assert.IsFalse(field.HasParseError, "đang nhập dở cặp ngày/giờ không phải lỗi");
            Assert.AreEqual(DisplayStyle.None, field.ErrorLabel.resolvedStyle.display, "không hiện \"Ô giờ còn trống\" khi con trỏ đang ở chính ô giờ");
            Assert.IsFalse(field.TimeInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));

            TypeText(field.TimeInput, "07:00");
            yield return MoveFocus(outside);
            Assert.AreEqual(new DateTime(2026, 9, 16, 7, 0, 0, DateTimeKind.Utc), field.value);
            Assert.AreEqual(1, changes.Count, "gõ nốt ô giờ rồi rời field = đúng một lần ghi");
            CollectionAssert.AreEqual(new[] { "2026-09-16|07:00" }, commits, "gõ nốt nửa còn lại = đúng một lần chốt");
            Assert.IsFalse(field.HasParseError);

            // Rời hẳn field khi ô giờ vẫn trống: lúc này mới báo và đưa chuỗi thô ra — đúng một lần.
            LiveOpsUtcDateTimeField second = new LiveOpsUtcDateTimeField();
            field.parent.Add(second);
            yield return ControlsTestPanel.WaitForLayout(second);
            List<string> secondCommits = RecordCommits(second);
            yield return MoveFocus(second.DateInput);
            TypeText(second.DateInput, "2026-09-16");
            yield return MoveFocus(second.TimeInput);
            Assert.AreEqual(0, secondCommits.Count);
            yield return MoveFocus(outside);
            Assert.IsTrue(second.HasParseError, "rời field khi cặp còn thiếu giờ phải báo lỗi");
            CollectionAssert.AreEqual(new[] { "2026-09-16|" }, secondCommits, "rời field thì phiên nhận chuỗi thô đúng một lần");
            Assert.AreEqual(LiveOpsHubStrings.UtcFieldTimeEmpty, second.ErrorLabel.text);
            Assert.IsTrue(second.TimeInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError), "viền đúng ô còn trống");
            Assert.IsFalse(second.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator UtcField_AssignLastReadableValue_ClearsBrokenText()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            yield return ControlsTestPanel.WaitForLayout(field);
            DateTime endUtc = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
            field.SetValueWithoutNotify(endUtc);
            List<(DateTime PreviousValue, DateTime NewValue)> changes = RecordChanges(field);

            field.DateInput.value = "2026-9-20";
            yield return null;
            Assert.IsTrue(field.HasParseError);
            Assert.AreEqual(endUtc, field.value, "giá trị vẫn là lần đọc được gần nhất — chính giá trị presenter sẽ gán sau \"Sửa thành\"");

            // "Sửa thành 2026-09-20 00:00": phiên ghi chuỗi chuẩn rồi presenter gán đúng giá trị đó — BaseField coi là gán trùng và bỏ qua.
            field.value = endUtc;
            yield return null;
            Assert.IsFalse(field.HasParseError, "gán giá trị khi ô đang giữ chuỗi hỏng phải thay chữ hỏng");
            Assert.AreEqual("2026-09-20", field.RawDateText);
            Assert.AreEqual("00:00", field.RawTimeText);
            Assert.IsFalse(field.DateInput.ClassListContains(LiveOpsHubClassNames.UtcFieldPartError), "hết viền lỗi");
            Assert.AreEqual(DisplayStyle.None, field.ErrorLabel.resolvedStyle.display, "hết dòng lỗi");
            Assert.AreEqual(0, changes.Count, "giá trị không đổi thì không bắn ChangeEvent — phiên không ghi lần nữa");

            field.value = endUtc.AddDays(1);
            Assert.AreEqual(1, changes.Count, "gán giá trị khác vẫn đi đường BaseField bình thường");
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

        /// <summary>
        /// (W9-30) Hàng giá trị (ô ngày + ô giờ + "UTC") phải rộng Y HỆT ở trạng thái sạch và trạng thái lỗi, và biểu
        /// tượng lỗi phải nằm ở DÒNG LỖI.
        /// <para>
        /// Vì sao đo bề rộng hàng chứ chỉ nhìn cây: hàng nở thêm khi có lỗi là đúng cơ chế đã đẩy biểu tượng ra khỏi pane
        /// 280px của inspector (đo ở 1280×760: hàng cần 177px trong 168px chỗ có). Mọi phần tử trong hàng đều
        /// <c>flex-shrink: 0</c> nên hàng nở ra là tràn, không phải bóp. Ca ở cấp control này khoá đúng nguyên nhân; ca
        /// trong pane thật (<c>CalendarInspectorUxFixTests</c>) khoá hậu quả.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator UtcField_ErrorState_DoesNotWidenTheValueRow()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            yield return ControlsTestPanel.WaitForLayout(field);
            field.SetRawTextWithoutNotify("2026-09-16", "12:00");
            yield return ControlsTestPanel.WaitForLayout(field);

            Assert.IsFalse(field.HasParseError, "mốc đầu của ca này là ô đọc được");
            Assert.IsTrue(field.ValueRow.Contains(field.ZoneLabel), "nhãn UTC thuộc hàng giá trị");
            Assert.IsTrue(field.ErrorRow.Contains(field.ErrorIcon),
                "biểu tượng lỗi thuộc DÒNG LỖI — để nó cuối hàng giá trị là dựng lại đúng chỗ tràn của W9-30");
            Assert.IsTrue(field.ErrorRow.Contains(field.ErrorLabel), "câu lỗi đứng cạnh biểu tượng của chính nó");
            Assert.IsFalse(field.ValueRow.Contains(field.ErrorIcon), "hàng giá trị không bao giờ chứa biểu tượng lỗi");
            Assert.AreEqual(DisplayStyle.None, field.ErrorRow.resolvedStyle.display,
                "không có lỗi thì cả dòng lỗi phải biến mất, không để lại một hàng cao 0 ăn lề");

            float cleanRowWidth = ContentWidthOf(field.ValueRow);
            Assert.Greater(cleanRowWidth, 0f, "hàng giá trị phải có phần tử — không thì ca này thành lời khai suông");

            field.SetRawTextWithoutNotify("2026-10-3", string.Empty);
            yield return ControlsTestPanel.WaitForLayout(field);

            Assert.IsTrue(field.HasParseError, "\"2026-10-3\" phải là chuỗi không đọc được");
            Assert.AreEqual(DisplayStyle.Flex, field.ErrorRow.resolvedStyle.display, "có lỗi thì dòng lỗi phải hiện");
            Assert.AreEqual(DisplayStyle.Flex, field.ErrorIcon.resolvedStyle.display);
            Assert.AreEqual(cleanRowWidth, ContentWidthOf(field.ValueRow), 0.5f,
                "hàng giá trị nở thêm khi có lỗi = đúng cơ chế đẩy biểu tượng ra ngoài pane 280px (W9-30)");
            LogAssert.NoUnexpectedReceived();
        }

        /// <summary>Mép phải xa nhất mà con của <paramref name="row"/> chiếm tới, tính từ mép trái của chính hàng.</summary>
        private static float ContentWidthOf(VisualElement row)
        {
            float rightEdge = 0f;
            for (int index = 0; index < row.childCount; index++)
            {
                VisualElement child = row[index];
                if (child.resolvedStyle.display == DisplayStyle.None) continue;
                if (child.layout.xMax > rightEdge) rightEdge = child.layout.xMax;
            }
            return rightEdge;
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

        /// <summary>Gõ chữ như bàn phím: <c>ITextEdition.UpdateText</c> (internal ở cả hai bản) đổi chữ và bắn InputEvent, chưa đổi value.</summary>
        private static void TypeText(TextField textField, string text)
        {
            MethodInfo updateText = typeof(ITextEdition).GetMethod("UpdateText", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(updateText, "ITextEdition.UpdateText không còn — đường gõ phím của TextField đã đổi, test phải viết lại");
            updateText.Invoke(textField.textEdition, new object[] { text });
            Assert.AreEqual(text, textField.text);
        }

        /// <summary>Chuyển focus thật qua FocusController (FocusOut → FocusIn → Blur): ô isDelayed chốt chữ trên đường này.</summary>
        private static IEnumerator MoveFocus(TextField target)
        {
            // focusedElement bị đổi đích về composite root ngoài cùng (chính LiveOpsUtcDateTimeField) nên không phân biệt được ô ngày
            // với ô giờ — xác nhận bằng FocusInEvent tới đúng ô đích.
            int focusInCount = 0;
            EventCallback<FocusInEvent> onFocusIn = focusInEvent => focusInCount++;
            target.RegisterCallback(onFocusIn);
            target.Focus();
            yield return null;
            target.UnregisterCallback(onFocusIn);
            Assert.Greater(focusInCount, 0, "focus phải vào ô đích — không có FocusInEvent thì test không đi đường chốt chữ thật");
        }

        /// <summary>
        /// Tách chuỗi thô của asset: nửa ngày người dùng gõ CÓ THỂ chứa dấu cách ("16 09 2026"), nên cắt ở dấu cách đầu tiên là
        /// xé đôi chữ người dùng gõ và ô giờ nhận "09 2026 12:00" (lỗi do chính vòng ghép/tách hai ô sinh ra).
        /// </summary>
        [TestCase("2026-09-16T12:00:00Z", "2026-09-16", "12:00:00")]
        [TestCase("2026-09-16 12:00", "2026-09-16", "12:00")]
        [TestCase("16 09 2026 12:00", "16 09 2026", "12:00")]
        [TestCase("16 09 2026", "16 09 2026", "")]
        [TestCase("2026-9-19 99:99", "2026-9-19", "99:99")]
        [TestCase("2026-09-16", "2026-09-16", "")]
        [TestCase("", "", "")]
        public void UtcField_SplitRawText_KeepsUserTextWhole(string rawText, string expectedDateText, string expectedTimeText)
        {
            LiveOpsUtcDateTimeField.SplitRawText(rawText, out string dateText, out string timeText);
            Assert.AreEqual(expectedDateText, dateText, "nửa ngày phải nguyên khối như người dùng gõ");
            Assert.AreEqual(expectedTimeText, timeText, "nửa giờ chỉ nhận đuôi thật sự là một lần gõ giờ");
        }

        /// <summary>Ghép rồi tách phải trả lại đúng hai nửa ban đầu — hai chiều của cùng một vòng, không được lệch nhau.</summary>
        [TestCase("16 09 2026", "12:00")]
        [TestCase("2026-10-3", "12:00")]
        [TestCase("2026-09-16", "")]
        public void UtcField_JoinThenSplit_RoundTrips(string dateText, string timeText)
        {
            string joined = LiveOpsUtcDateTimeField.JoinRawText(dateText, timeText);
            LiveOpsUtcDateTimeField.SplitRawText(joined, out string splitDateText, out string splitTimeText);
            Assert.AreEqual(dateText, splitDateText);
            Assert.AreEqual(timeText, splitTimeText);
        }

        /// <summary>Chuỗi ghi vào asset: dạng chuẩn khi đọc được, nguyên văn khi không — một chỗ quyết, nơi nghe không tự ghép.</summary>
        [TestCase("2026-09-16", "12:00", "2026-09-16T12:00:00Z")]
        [TestCase("2026-10-3", "12:00", "2026-10-3 12:00")]
        [TestCase("2026-09-16", "", "2026-09-16")]
        public void UtcField_ToAssetText_UsesCanonicalOnlyWhenReadable(string dateText, string timeText, string expected)
        {
            Assert.AreEqual(expected, LiveOpsUtcDateTimeField.ToAssetText(dateText, timeText));
        }

        /// <summary>
        /// (W9-19) Ô ngày phải còn KHE HỞ cho chữ dài nhất, không chỉ "chưa cắt". Đo bằng chính font của ô (class mono nằm trên
        /// ô, nên <c>MeasureTextSize</c> ở đây trả đúng con số người dùng thấy) và so với vùng NỘI DUNG của TextInput — vùng
        /// sau khi trừ đệm, tức đúng chỗ Unity cắt chữ.
        /// </summary>
        [UnityTest]
        public IEnumerator UtcField_DateInput_KeepsSlackForTheLongestDateText()
        {
            LiveOpsUtcDateTimeField field = CreateField(false);
            yield return ControlsTestPanel.WaitForLayout(field);
            field.SetRawTextWithoutNotify(LongestDateText, "12:00");
            yield return null;

            VisualElement input = InputOf(field.DateInput);
            Assert.IsNotNull(input, "không tìm thấy TextInput của ô ngày — không có chỗ nào đo được vùng nội dung thật");
            float needed = field.DateInput.MeasureTextSize(LongestDateText, 0f, VisualElement.MeasureMode.Undefined,
                0f, VisualElement.MeasureMode.Undefined).x;
            float available = input.contentRect.width;

            Assert.Greater(needed, 0f, "đo chữ trả 0px — phép đo hỏng thì ca này thành lời khai suông");
            Assert.GreaterOrEqual(available + TextMeasureTolerance, needed + (2f * RequiredTextSlackPerSide),
                "chữ ngày \"" + LongestDateText + "\" cần " + needed.ToString("0.#", CultureInfo.InvariantCulture)
                + "px, vùng nội dung của ô có " + available.ToString("0.#", CultureInfo.InvariantCulture)
                + "px — NGƯỠNG là dư " + RequiredTextSlackPerSide.ToString("0.#", CultureInfo.InvariantCulture)
                + "px mỗi bên (tổng " + (2f * RequiredTextSlackPerSide).ToString("0.#", CultureInfo.InvariantCulture)
                + "px) để một đổi metric font không cắt chữ trong im lặng, cộng "
                + TextMeasureTolerance.ToString("0.#", CultureInfo.InvariantCulture)
                + "px DUNG SAI ĐO của MeasureTextSize (W9-19)");
            LogAssert.NoUnexpectedReceived();
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

        /// <summary>Ghi lại MỌI lần chốt chữ — đường chốt của ô là một, chữ đọc được hay không cũng đi qua đây.</summary>
        private static List<string> RecordCommits(LiveOpsUtcDateTimeField field)
        {
            List<string> commits = new List<string>();
            field.TextCommitted += (dateText, timeText) => commits.Add(dateText + "|" + timeText);
            return commits;
        }
    }
}
