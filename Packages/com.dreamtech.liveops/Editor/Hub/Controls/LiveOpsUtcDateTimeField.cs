using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Ô giờ UTC của inspector đợt, popover Thêm đợt và Neo luật lặp ([FD §2.15], [SD1 §3.1]): ô ngày 76px <c>yyyy-MM-dd</c> + ô giờ
    /// 44px <c>HH:mm</c> + nhãn "UTC" + dòng phụ giờ máy ("19:00 16/9 giờ máy"). Hai ô riêng thay cho một hộp chuỗi ISO vì chuỗi ISO
    /// mời lỗi "2026-10-3" và không cho thấy đang gõ giờ UTC hay giờ máy ([FD] bảng lỗi hệ cũ #7).
    ///
    /// Chuỗi không đọc được KHÔNG bị nuốt hay tự sửa: ô giữ nguyên chữ người dùng gõ ("2026-10-3"), viền blocked-fill, dòng lỗi 10px
    /// nêu dạng đúng, và <see cref="RawTextCommitted"/> đưa chuỗi thô cho phiên lịch ghi nguyên văn vào asset — để luật
    /// <c>utc-time-format</c> báo đúng chuỗi và "Sửa an toàn" đề xuất đúng giá trị. Đọc chặt (<c>yyyy-MM-dd</c>, <c>HH:mm</c> hoặc
    /// <c>HH:mm:ss</c>): thiếu số 0 là lỗi ở ô, dù <see cref="LiveEventUtcText.TryNormalize"/> hiểu được — sửa được thì là việc của
    /// đề xuất, không phải của ô.
    ///
    /// Ghi khi rời ô hoặc Enter (<c>isDelayed</c>), không theo từng phím: gõ dở "2026-1" không được thành một lần sửa asset. Cùng lý do,
    /// Tab từ ô ngày vừa gõ sang ô giờ còn trống (hay ngược lại) KHÔNG ghi và KHÔNG báo lỗi — người dùng đang nhập dở cặp ngày/giờ; ô
    /// chỉ báo "còn trống" và đưa chuỗi thô ra khi focus rời hẳn field mà cặp vẫn thiếu một nửa.
    /// </summary>
#if UNITY_2023_2_OR_NEWER
    [UxmlElement]
#endif
    public sealed partial class LiveOpsUtcDateTimeField : BaseField<DateTime>
    {
        internal const string DateFormat = "yyyy-MM-dd";
        private static readonly string[] TimeFormats = { "hh\\:mm", "hh\\:mm\\:ss" };
        private const string TimeWithoutSecondsFormat = "HH:mm";
        private const string TimeWithSecondsFormat = "HH:mm:ss";
        private const int ErrorIconSize = 12;

        private readonly LiveOpsPlaceholder _datePlaceholder;
        private readonly LiveOpsPlaceholder _timePlaceholder;
        private bool _showDeviceTimeLine = true;
        private TimeSpan _deviceOffset = TimeSpan.Zero;
        private bool _hasValue;
        private bool _hasParseError;
        private bool _errorTextFromCaller;
        // Một ô con vừa ghi mà ô kia còn trống, trong lúc focus vẫn ở trong field: chờ người dùng gõ nốt thay vì ghi nửa cặp vào asset.
        private bool _pendingIncompleteInput;
        // Lần FocusOutEvent gần nhất chuyển focus sang một phần của chính field (Tab ngày → giờ). Đọc trong lần ghi của ô con ngay sau:
        // 2022.3 ghi ô isDelayed lúc BlurEvent, 6000.6 lúc FocusOutEvent — cả hai đều SAU pha trickle-down của FocusOutEvent trên field.
        private bool _focusMovingWithinField;

        public LiveOpsUtcDateTimeField() : this(null)
        {
        }

        public LiveOpsUtcDateTimeField(string label) : this(label, new VisualElement())
        {
        }

        private LiveOpsUtcDateTimeField(string label, VisualElement input) : base(label, input)
        {
            AddToClassList(LiveOpsHubClassNames.UtcField);
            InputContainer = input;
            input.AddToClassList(LiveOpsHubClassNames.UtcFieldInput);

            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.UtcFieldRow);
            input.Add(row);

            DateInput = CreatePart(LiveOpsHubClassNames.UtcFieldDate);
            TimeInput = CreatePart(LiveOpsHubClassNames.UtcFieldTime);
            row.Add(DateInput);
            row.Add(TimeInput);
            _datePlaceholder = LiveOpsPlaceholder.Attach(DateInput, LiveOpsHubStrings.UtcFieldDatePlaceholder);
            _timePlaceholder = LiveOpsPlaceholder.Attach(TimeInput, LiveOpsHubStrings.UtcFieldTimePlaceholder);

            ZoneLabel = new Label(LiveOpsHubStrings.UtcLabel) { pickingMode = PickingMode.Ignore };
            ZoneLabel.AddToClassList(LiveOpsHubClassNames.UtcFieldZone);
            row.Add(ZoneLabel);

            ErrorIcon = LiveOpsHubIcons.CreateImage(ErrorIconName, ErrorIconSize);
            ErrorIcon.AddToClassList(LiveOpsHubClassNames.UtcFieldErrorIcon);
            row.Add(ErrorIcon);

            DeviceTimeLabel = new Label();
            DeviceTimeLabel.AddToClassList(LiveOpsHubClassNames.UtcFieldDeviceLine);
            input.Add(DeviceTimeLabel);

            ErrorLabel = new Label();
            ErrorLabel.AddToClassList(LiveOpsHubClassNames.UtcFieldError);
            ErrorLabel.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            input.Add(ErrorLabel);

            DateInput.RegisterValueChangedCallback(OnPartCommitted);
            TimeInput.RegisterValueChangedCallback(OnPartCommitted);
            // Ô isDelayed không đổi value khi gõ nên LiveOpsPlaceholder (ẩn theo value) sẽ để gợi ý "yyyy-MM-dd" đè lên chữ đang gõ tới
            // khi Enter/rời ô. InputEvent bắn theo từng lần sửa chữ ở cả hai bản (TextElement.UpdateText) — ẩn/hiện theo chữ đang có.
            DateInput.RegisterCallback<InputEvent>(inputEvent => SetPlaceholderHidden(_datePlaceholder, inputEvent.newData));
            TimeInput.RegisterCallback<InputEvent>(inputEvent => SetPlaceholderHidden(_timePlaceholder, inputEvent.newData));
            RegisterCallback<FocusOutEvent>(OnFocusOutTrickleDown, TrickleDown.TrickleDown);
            RefreshDecorations();
        }

        /// <summary>Thuộc tính UXML <c>show-device-time-line</c>: hiện dòng phụ giờ máy dưới hai ô (mặc định bật).</summary>
#if UNITY_2023_2_OR_NEWER
        [UxmlAttribute]
#endif
        public bool ShowDeviceTimeLine
        {
            get => _showDeviceTimeLine;
            set
            {
                _showDeviceTimeLine = value;
                RefreshDecorations();
            }
        }

        /// <summary>Chữ đang ở ô ngày — giữ nguyên "2026-10-3" khi không đọc được.</summary>
        public string RawDateText => DateInput.value ?? string.Empty;

        /// <summary>Chữ đang ở ô giờ.</summary>
        public string RawTimeText => TimeInput.value ?? string.Empty;

        /// <summary>Chữ trong ô không thành một giờ UTC — <see cref="BaseField{T}.value"/> vẫn là giá trị đọc được gần nhất.</summary>
        public bool HasParseError => _hasParseError;

        /// <summary>(ngày, giờ) nguyên văn khi người dùng ghi chuỗi không đọc được — phiên lịch vẫn ghi chuỗi thô vào asset.</summary>
        public event Action<string, string> RawTextCommitted;

        /// <summary>
        /// Gán giá trị. <see cref="BaseField{T}"/> bỏ qua lần gán bằng giá trị hiện tại, mà khi ô đang giữ chuỗi hỏng thì giá trị hiện tại
        /// chính là lần đọc được gần nhất: presenter "Sửa thành 2026-09-20 00:00" gán lại đúng giá trị đó sẽ là no-op — chữ hỏng, viền
        /// và dòng lỗi còn nguyên trong khi asset đã đúng. Trường hợp này áp thẳng chữ chuẩn, không bắn ChangeEvent (giá trị không đổi).
        /// </summary>
        public override DateTime value
        {
            get => base.value;
            set
            {
                if ((_hasParseError || _pendingIncompleteInput) && value == base.value)
                {
                    SetValueWithoutNotify(value);
                    return;
                }
                base.value = value;
            }
        }

        internal TextField DateInput { get; }
        internal TextField TimeInput { get; }
        internal VisualElement InputContainer { get; }
        internal Label ZoneLabel { get; }
        internal Image ErrorIcon { get; }
        internal Label DeviceTimeLabel { get; }
        internal Label ErrorLabel { get; }
        internal LiveOpsPlaceholder DatePlaceholder => _datePlaceholder;
        internal LiveOpsPlaceholder TimePlaceholder => _timePlaceholder;

        private const string ErrorIconName = "console.erroricon.sml";

        /// <summary>
        /// Đặt chữ thô từ asset (chuỗi đợt đang lưu có thể hỏng sẵn) mà không bắn sự kiện. Đọc được thì cập nhật giá trị; không đọc
        /// được thì hiện lỗi mặc định của ô — presenter muốn câu của phát hiện thì gọi <see cref="SetErrorText"/> sau.
        /// </summary>
        public void SetRawTextWithoutNotify(string dateText, string timeText)
        {
            DateInput.SetValueWithoutNotify(dateText ?? string.Empty);
            TimeInput.SetValueWithoutNotify(timeText ?? string.Empty);
            _pendingIncompleteInput = false;
            RefreshPlaceholders();
            if (TryParseParts(RawDateText, RawTimeText, out DateTime parsed))
            {
                ApplyValue(parsed);
                return;
            }
            ShowParseError();
        }

        /// <summary>Độ lệch giờ máy (port múi giờ tiêm vào — test cố định +7) cho dòng phụ "07:00 18/9 giờ máy".</summary>
        public void SetDeviceOffset(TimeSpan deviceOffset)
        {
            _deviceOffset = deviceOffset;
            RefreshDecorations();
        }

        /// <summary>
        /// Viền blocked-fill + dòng lỗi 10px với câu của người gọi (vd câu phát hiện <c>utc-time-format</c>); rỗng/null = bỏ lỗi do
        /// người gọi đặt (lỗi đọc chuỗi của chính ô vẫn giữ tới khi chữ đọc được).
        /// </summary>
        public void SetErrorText(string errorText)
        {
            if (string.IsNullOrEmpty(errorText))
            {
                _errorTextFromCaller = false;
                if (_hasParseError)
                {
                    ErrorLabel.text = DescribeParseError(RawDateText, RawTimeText);
                }
                else
                {
                    ErrorLabel.text = string.Empty;
                }
            }
            else
            {
                _errorTextFromCaller = true;
                ErrorLabel.text = errorText;
            }
            RefreshDecorations();
        }

        public override void SetValueWithoutNotify(DateTime newValue)
        {
            DateTime utc = DateTime.SpecifyKind(newValue, DateTimeKind.Utc);
            base.SetValueWithoutNotify(utc);
            _hasValue = true;
            _hasParseError = false;
            _pendingIncompleteInput = false;
            DateInput.SetValueWithoutNotify(utc.ToString(DateFormat, CultureInfo.InvariantCulture));
            string timeFormat = utc.Second == 0 && utc.Millisecond == 0 ? TimeWithoutSecondsFormat : TimeWithSecondsFormat;
            TimeInput.SetValueWithoutNotify(utc.ToString(timeFormat, CultureInfo.InvariantCulture));
            RefreshPlaceholders();
            if (!_errorTextFromCaller) ErrorLabel.text = string.Empty;
            RefreshDecorations();
        }

        /// <summary>Đọc chặt hai ô thành một giờ UTC; false khi một ô trống hoặc sai dạng.</summary>
        internal static bool TryParseParts(string dateText, string timeText, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrEmpty(dateText)) return false;
            if (!DateTime.TryParseExact(dateText.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date)) return false;
            if (!TryParseTimeOfDay(timeText, out TimeSpan timeOfDay)) return false;
            utc = DateTime.SpecifyKind(date.Date + timeOfDay, DateTimeKind.Utc);
            return true;
        }

        /// <summary>
        /// Tách chuỗi giờ THÔ của asset thành (ngày, giờ) cho hai ô. Nhận CẢ dấu ngăn "T" của ISO LẪN dấu cách: khi chuỗi không
        /// đọc được, inspector ghi nguyên văn thứ người dùng gõ và ghép hai nửa bằng DẤU CÁCH ("2026-09-1 12:00"), nên bộ tách chỉ
        /// biết "T" sẽ dồn cả chuỗi vào ô ngày và để ô giờ trống — người dùng thấy giờ mình KHÔNG đụng tới tự biến mất, Dài về 0 và
        /// câu lỗi vừa trích "12:00" vừa bảo "Ô giờ còn trống" (UJ-04).
        /// Phần giây giữ nguyên (<c>HH:mm:ss</c> vẫn đọc được): cắt còn <c>HH:mm</c> là lặng lẽ đổi giờ của đợt khi asset có giây.
        /// </summary>
        internal static void SplitRawText(string rawText, out string dateText, out string timeText)
        {
            dateText = string.Empty;
            timeText = string.Empty;
            if (string.IsNullOrEmpty(rawText)) return;
            string text = rawText.Trim();
            if (text.Length == 0) return;
            int separatorIndex = text.IndexOf('T');
            if (separatorIndex < 0) separatorIndex = text.IndexOf(' ');
            if (separatorIndex < 0)
            {
                dateText = text;
                return;
            }
            dateText = text.Substring(0, separatorIndex).Trim();
            timeText = text.Substring(separatorIndex + 1).Trim().TrimEnd('Z').Trim();
        }

        /// <summary>
        /// Câu lỗi mặc định: nói chuỗi nào không đọc được và dạng cần gõ; ngày thiếu số 0 thì nêu luôn cách viết đúng. Hai ô cùng hỏng thì
        /// nêu cả hai — chỉ nêu ô ngày thì lỗi giờ chỉ lộ ra sau khi sửa xong ngày, người dùng phải sửa hai lượt.
        /// </summary>
        internal static string DescribeParseError(string dateText, string timeText)
        {
            string date = dateText ?? string.Empty;
            string time = timeText ?? string.Empty;
            string dateError = DescribeDateError(date);
            string timeError = IsTimeReadable(time) ? null : DescribeTimeError(time);
            if (dateError == null) return timeError ?? string.Empty;
            return timeError == null ? dateError : dateError + " " + timeError;
        }

        private static string DescribeDateError(string date)
        {
            if (IsDateReadable(date)) return null;
            if (date.Trim().Length == 0) return LiveOpsHubStrings.UtcFieldDateEmpty;
            // Chuỗi người dùng gõ vào Label rich text: "2026-10-<b>3" không được thành chữ đậm hay nuốt thẻ — bọc noparse như mọi giá
            // trị thô của hub (7.6), để dòng lỗi nêu đúng từng ký tự đã gõ.
            string literal = LiveOpsJsonView.ProtectRawText(date);
            if (LiveEventUtcText.TryNormalize(date, out string canonical) && LiveEventUtcText.TryParse(canonical, out DateTime normalized))
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.UtcFieldDateUnreadableWithSuggestionFormat, literal,
                    normalized.ToString(DateFormat, CultureInfo.InvariantCulture));
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.UtcFieldDateUnreadableFormat, literal);
        }

        private static string DescribeTimeError(string time)
        {
            if (time.Trim().Length == 0) return LiveOpsHubStrings.UtcFieldTimeEmpty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.UtcFieldTimeUnreadableFormat, LiveOpsJsonView.ProtectRawText(time));
        }

        private static bool IsDateReadable(string dateText)
        {
            if (string.IsNullOrEmpty(dateText)) return false;
            return DateTime.TryParseExact(dateText.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime _);
        }

        private static bool IsTimeReadable(string timeText)
        {
            return TryParseTimeOfDay(timeText, out TimeSpan _);
        }

        /// <summary>Đọc riêng nửa giờ (<c>HH:mm</c> hoặc <c>HH:mm:ss</c>) — đúng hai dạng mà ô giờ nhận, một chỗ khai.</summary>
        internal static bool TryParseTimeOfDay(string timeText, out TimeSpan timeOfDay)
        {
            timeOfDay = default;
            if (string.IsNullOrEmpty(timeText)) return false;
            if (!TimeSpan.TryParseExact(timeText.Trim(), TimeFormats, CultureInfo.InvariantCulture, out timeOfDay)) return false;
            return timeOfDay >= TimeSpan.Zero && timeOfDay < TimeSpan.FromDays(1);
        }

        private TextField CreatePart(string className)
        {
            TextField part = new TextField { isDelayed = true };
            part.AddToClassList(className);
            part.AddToClassList(LiveOpsHubClassNames.Mono);
            return part;
        }

        private void OnPartCommitted(ChangeEvent<string> changeEvent)
        {
            // ChangeEvent<string> của ô con không được nổi lên như giá trị của field — nơi nghe chỉ dùng ChangeEvent<DateTime>.
            changeEvent.StopPropagation();
            RefreshPlaceholders();
            // Text element bên trong ô cũng bắn ChangeEvent<string> (Esc trả lại chữ cũ) và nó nổi tới đây — chỉ lần ghi value của chính
            // ô con mới là một lần người dùng chốt chữ.
            if (changeEvent.target != DateInput && changeEvent.target != TimeInput) return;
            bool focusStaysInField = _focusMovingWithinField;
            _focusMovingWithinField = false;

            if (TryParseParts(RawDateText, RawTimeText, out DateTime parsed))
            {
                bool wasBroken = _hasParseError;
                DateTime previous = value;
                ApplyValue(parsed);
                if (previous != parsed)
                {
                    SendDateTimeChange(previous, parsed);
                }
                else if (wasBroken)
                {
                    // Chữ hỏng được sửa về đúng giá trị cũ: asset vẫn đang giữ chuỗi thô → phải báo để phiên ghi lại chuỗi chuẩn.
                    SendDateTimeChange(previous, parsed);
                }
                return;
            }

            if (focusStaysInField && IsIncomplete())
            {
                // Tab từ ô vừa gõ sang ô còn trống: ghi nửa cặp thành chuỗi thô là một bước Undo + phát hiện utc-time-format nháy lên
                // trước khi người dùng kịp gõ nửa kia. Chờ tới lần ghi sau hoặc tới khi focus rời hẳn field (OnFocusOutTrickleDown).
                _pendingIncompleteInput = true;
                if (_hasParseError) ShowParseError();
                return;
            }
            ReportUnreadableInput();
        }

        private void OnFocusOutTrickleDown(FocusOutEvent focusOutEvent)
        {
            VisualElement nextFocused = focusOutEvent.relatedTarget as VisualElement;
            _focusMovingWithinField = nextFocused != null && (nextFocused == this || Contains(nextFocused));
            if (_focusMovingWithinField || !_pendingIncompleteInput) return;
            // Rời hẳn field khi cặp vẫn thiếu một nửa. Ô đang rời còn chữ chưa ghi thì lần ghi của nó chạy ngay sau (cờ đã false) và tự báo;
            // báo ở đây nữa là hai lần RawTextCommitted cho cùng một lần rời ô.
            if (HasUncommittedText(DateInput) || HasUncommittedText(TimeInput)) return;
            ReportUnreadableInput();
        }

        private static bool HasUncommittedText(TextField part)
        {
            return !string.Equals(part.text ?? string.Empty, part.value ?? string.Empty, StringComparison.Ordinal);
        }

        private bool IsIncomplete()
        {
            return RawDateText.Trim().Length == 0 || RawTimeText.Trim().Length == 0;
        }

        private void ReportUnreadableInput()
        {
            _pendingIncompleteInput = false;
            ShowParseError();
            RawTextCommitted?.Invoke(RawDateText, RawTimeText);
        }

        private void RefreshPlaceholders()
        {
            SetPlaceholderHidden(_datePlaceholder, DateInput.text);
            SetPlaceholderHidden(_timePlaceholder, TimeInput.text);
        }

        private static void SetPlaceholderHidden(LiveOpsPlaceholder placeholder, string text)
        {
            placeholder.EnableInClassList(LiveOpsHubClassNames.PlaceholderHidden, !string.IsNullOrEmpty(text));
        }

        private void ApplyValue(DateTime parsed)
        {
            _hasParseError = false;
            SetValueWithoutNotify(parsed);
        }

        private void ShowParseError()
        {
            _hasParseError = true;
            if (!_errorTextFromCaller) ErrorLabel.text = DescribeParseError(RawDateText, RawTimeText);
            RefreshDecorations();
        }

        private void SendDateTimeChange(DateTime previous, DateTime next)
        {
            using (ChangeEvent<DateTime> changeEvent = ChangeEvent<DateTime>.GetPooled(previous, next))
            {
                changeEvent.target = this;
                SendEvent(changeEvent);
            }
        }

        private void RefreshDecorations()
        {
            // Viền đúng từng ô không đọc được (cả hai khi cả hai hỏng), khớp câu của DescribeParseError.
            bool dateBroken = _hasParseError && !IsDateReadable(RawDateText);
            bool timeBroken = _hasParseError && !IsTimeReadable(RawTimeText);
            bool showError = _hasParseError || _errorTextFromCaller;
            // Lỗi do người gọi đặt mà chữ vẫn đọc được: viền cả ô ngày (thứ người dùng sửa trước) để không có lỗi mà không có chỗ nhìn.
            DateInput.EnableInClassList(LiveOpsHubClassNames.UtcFieldPartError, dateBroken || (_errorTextFromCaller && !_hasParseError));
            TimeInput.EnableInClassList(LiveOpsHubClassNames.UtcFieldPartError, timeBroken);
            ErrorIcon.EnableInClassList(LiveOpsHubClassNames.UtcFieldHidden, !showError);
            ErrorLabel.EnableInClassList(LiveOpsHubClassNames.UtcFieldHidden, !showError || string.IsNullOrEmpty(ErrorLabel.text));

            bool showDeviceLine = _showDeviceTimeLine && _hasValue && !_hasParseError;
            DeviceTimeLabel.text = showDeviceLine ? new LiveOpsHubFormat(_deviceOffset).DeviceTimeLine(value) : string.Empty;
            DeviceTimeLabel.EnableInClassList(LiveOpsHubClassNames.UtcFieldHidden, !showDeviceLine);
        }

#if !UNITY_2023_2_OR_NEWER
        public new sealed class UxmlFactory : UxmlFactory<LiveOpsUtcDateTimeField, UxmlTraits>
        {
        }

        public new sealed class UxmlTraits : BaseField<DateTime>.UxmlTraits
        {
            // Tên thuộc tính trùng tên kebab-case mà [UxmlAttribute] của Unity 6 sinh từ property (ShowDeviceTimeLine → "show-device-time-line");
            // "label" đọc bởi traits của BaseField như Unity 6.
            private readonly UxmlBoolAttributeDescription _showDeviceTimeLine =
                new UxmlBoolAttributeDescription { name = "show-device-time-line", defaultValue = true };

            public override void Init(VisualElement visualElement, IUxmlAttributes bag, CreationContext context)
            {
                base.Init(visualElement, bag, context);
                ((LiveOpsUtcDateTimeField)visualElement).ShowDeviceTimeLine = _showDeviceTimeLine.GetValueFromBag(bag, context);
            }
        }
#endif
    }

#if UNITY_2023_2_OR_NEWER
    /// <summary>
    /// Unity 6 khai thuộc tính UXML <c>value</c> cho mọi <c>BaseField&lt;T&gt;</c> có <c>[UxmlElement]</c>; không có bộ đổi cho
    /// <see cref="DateTime"/> thì mỗi lần nạp domain Unity log Error "define a custom UxmlAttributeConverter&lt;DateTime&gt;" (thấy ở lượt
    /// import của gói) — log lạ làm đỏ test có <c>LogAssert.NoUnexpectedReceived</c>. Bộ đổi chỉ để tắt lỗi đó: đặt giá trị bằng UXML
    /// KHÔNG phải hợp đồng của ô (đo ở 6000.6: <c>value="…"</c> không tới được ô; 2022.3 không có đường đọc) — giá trị luôn đặt từ C#
    /// (<see cref="LiveOpsUtcDateTimeField.SetRawTextWithoutNotify"/> / <c>SetValueWithoutNotify</c>).
    /// </summary>
    internal sealed class LiveOpsUtcDateTimeUxmlConverter : UnityEditor.UIElements.UxmlAttributeConverter<DateTime>
    {
        public override DateTime FromString(string value)
        {
            return LiveEventUtcText.TryParse(value, out DateTime utc) ? utc : default;
        }

        public override string ToString(DateTime value)
        {
            return LiveEventUtcText.Format(value);
        }
    }
#endif
}
