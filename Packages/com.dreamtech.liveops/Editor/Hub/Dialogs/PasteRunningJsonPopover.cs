using System;
using System.Collections.Generic;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Hai chế độ của popover dán — cùng một ô dán, khác việc sẽ làm sau khi bấm nút chính.</summary>
    internal enum PasteRunningJsonMode
    {
        /// <summary>Đã có asset: dán để so, hoặc thay nháp bằng bản dán ([SD2 §2.9]).</summary>
        PasteIntoOpenCalendar = 0,

        /// <summary>Chưa có asset: dán để TẠO asset mới từ bản đang chạy (V-14).</summary>
        ImportIntoNewAsset = 1,
    }

    /// <summary>Lựa chọn của <c>RadioButtonGroup</c> ở chế độ dán; thứ tự = thứ tự vẽ, không đổi số.</summary>
    internal enum PasteRunningJsonChoice
    {
        CompareOnly = 0,
        ReplaceDraft = 1,
    }

    /// <summary>Thứ popover giao lại cho luồng khi người dùng bấm nút chính — popover không tự đụng phiên.</summary>
    internal sealed class PasteRunningJsonSubmission
    {
        internal PasteRunningJsonSubmission(string pastedText, LiveEventCalendarDocument document, int formatVersion,
            PasteRunningJsonChoice choice, bool stampAsPublished)
        {
            PastedText = pastedText ?? string.Empty;
            Document = document;
            FormatVersion = formatVersion;
            Choice = choice;
            StampAsPublished = stampAsPublished;
        }

        /// <summary>Chuỗi dán NGUYÊN VĂN — sha và bản chụp của dấu đã đăng tính trên chính chuỗi này, không phải bản viết lại.</summary>
        public string PastedText { get; }

        /// <summary>Tài liệu parser của game đọc ra; không bao giờ null (nút chính khoá khi chưa đọc được).</summary>
        public LiveEventCalendarDocument Document { get; }

        public int FormatVersion { get; }
        public PasteRunningJsonChoice Choice { get; }

        /// <summary>Toggle "Ghi dấu đây là bản đang chạy" của chế độ nhập; luôn false ở chế độ dán.</summary>
        public bool StampAsPublished { get; }
    }

    /// <summary>
    /// Popover "Dán JSON đang chạy" / "Nhập JSON đang chạy" ([SD2 §2.9], vá V-14). Ba luật của thiết kế:
    /// <list type="bullet">
    /// <item><b>Kiểm ngay khi gõ</b> — cú pháp qua <see cref="LiveOpsJsonSyntaxLocator"/> (có dòng/ký tự) rồi mới tới parser
    /// thật qua port <see cref="ILiveOpsHubJsonReadBack"/>. Không đọc được thì nút chính khoá và lý do IN THÀNH CHỮ cạnh nút
    /// (SPIKE-B SP-3), và KHÔNG BAO GIỜ tạo asset hay đụng nháp.</item>
    /// <item><b>Mặc định là việc an toàn</b> — lựa chọn "Chỉ so sánh với nháp" được chọn sẵn; "Thay nháp…" phải qua hộp xác
    /// nhận cấp 1 do luồng mở, không phải popover tự áp.</item>
    /// <item><b>Popover không đụng phiên</b> — nó trả <see cref="PasteRunningJsonSubmission"/> cho luồng
    /// (<c>LiveOpsHubPasteRunningJsonAction</c>); chỗ áp Undo group, toast và ghi asset chỉ có một.</item>
    /// </list>
    /// </summary>
    internal sealed class PasteRunningJsonPopover : LiveOpsPopoverContent
    {
        /// <summary>
        /// 320px theo khuôn popover của hub ([FD §7]). Chiều cao 280: đo trên cây thật thì nội dung cao nhất (chế độ
        /// dán, có nhóm lựa chọn) là 257; 23px còn lại là chỗ cho bản tiếng Anh làm dòng trạng thái hoặc nhãn Toggle xuống
        /// thêm một dòng. Chỗ thừa dồn xuống hàng nút (margin-top: auto) chứ không làm ô dán phình ra — ô dán luôn đúng
        /// 96px nên ảnh chụp đo lại được số đó.
        /// <para>
        /// Số 264 của lượt trước chưa từng được đo với nội dung thật (soát W5 P-3) và ô dán lúc đó cao "theo nội dung" nên
        /// hàng nút rơi khỏi cửa sổ ngay khi dán một bản đang chạy thật; test
        /// <c>Layout_LongJsonPasted_ButtonsStayInsidePopoverWindow</c> giờ khoá cả hai số.
        /// </para>
        /// </summary>
        private static readonly Vector2 WindowSize = new Vector2(320f, 280f);

        /// <summary>Sheet riêng của popover dán — bốn sheet chung do <c>LiveOpsPopoverContent</c> nạp ở gốc.</summary>
        private static readonly string[] PopoverOwnSheets = { LiveOpsHubPaths.PasteRunningJsonPopoverUss };

        private readonly PasteRunningJsonMode _mode;
        private readonly string _remoteConfigKey;
        private readonly ILiveOpsHubJsonReadBack _jsonReadBack;
        private readonly ILiveOpsHubLayoutLoader _layoutLoader;
        private readonly LiveOpsHubFormat _format;
        private readonly Func<LiveEventCalendarDocument, int> _countUndeclaredEventTypes;
        private readonly Action<PasteRunningJsonSubmission> _submit;

        private TextField _inputField;
        private Label _statusLabel;
        private RadioButtonGroup _modeGroup;
        private Toggle _stampToggle;
        private LiveOpsButtonSlot _confirmSlot;

        private LiveEventCalendarDocument _readDocument;
        private int _readFormatVersion;

        /// <param name="format">
        /// Dòng "Đọc được: …" in số bằng đúng bộ định dạng của hub — hai chỗ đếm cùng một thứ (dòng này và câu outcome sau khi
        /// nhập) mà in "1000" với "1.000" thì người dùng tưởng là hai con số khác nhau.
        /// </param>
        /// <param name="countUndeclaredEventTypes">
        /// Đếm loại có trong bản dán mà asset đang mở chưa khai báo — luồng biết định nghĩa loại, popover thì không.
        /// </param>
        internal PasteRunningJsonPopover(PasteRunningJsonMode mode, string remoteConfigKey, ILiveOpsHubJsonReadBack jsonReadBack,
            ILiveOpsHubLayoutLoader layoutLoader, LiveOpsHubFormat format,
            Func<LiveEventCalendarDocument, int> countUndeclaredEventTypes, Action<PasteRunningJsonSubmission> submit)
        {
            _mode = mode;
            _remoteConfigKey = string.IsNullOrEmpty(remoteConfigKey) ? LiveEventCalendarDocument.DefaultRemoteConfigKey : remoteConfigKey;
            _jsonReadBack = jsonReadBack ?? throw new ArgumentNullException(nameof(jsonReadBack));
            _layoutLoader = layoutLoader ?? throw new ArgumentNullException(nameof(layoutLoader));
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _countUndeclaredEventTypes = countUndeclaredEventTypes;
            _submit = submit;
        }

        /// <summary>Số lần nút chính đã chạy — test chứng minh JSON hỏng thì không luồng nào khởi động.</summary>
        internal int SubmitCount { get; private set; }

        internal TextField InputField => _inputField;
        internal Label StatusLabel => _statusLabel;
        internal RadioButtonGroup ModeGroup => _modeGroup;
        internal Toggle StampToggle => _stampToggle;
        internal LiveOpsButtonSlot ConfirmSlot => _confirmSlot;
        internal Button ConfirmButton => _confirmSlot == null ? null : _confirmSlot.Button;
        internal Button CancelButton { get; private set; }

        protected override Vector2 PopoverSize => WindowSize;

        /// <summary>Ô dán nhận focus đầu tiên: việc đầu tiên phải làm là ⌘V, không phải bấm nút.</summary>
        protected override Focusable InitialFocus => _inputField;

        /// <summary>Test đặt chuỗi như người dùng dán (đi qua đúng đường kiểm lại của <see cref="RefreshStatus"/>).</summary>
        internal void PasteForTest(string pastedText)
        {
            if (_inputField == null) return;
            _inputField.value = pastedText ?? string.Empty;
            RefreshStatus();
        }

        internal void SelectChoiceForTest(PasteRunningJsonChoice choice)
        {
            if (_modeGroup == null) return;
            _modeGroup.value = (int)choice;
        }

        internal void ClickConfirmForTest()
        {
            OnConfirmClicked();
        }

        protected override VisualElement BuildContent()
        {
            VisualTreeAsset layout = _layoutLoader.LoadVisualTree(LiveOpsHubPaths.PasteRunningJsonPopoverUxml);
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.PasteMissingLayoutFormat, LiveOpsHubPaths.PasteRunningJsonPopoverUxml));
            }

            // Bỏ vỏ TemplateContainer (cùng lý do với ProposalPopover): vỏ không mang class nên nó không nhận bề rộng 320px.
            VisualElement root = layout.Instantiate().Q(LiveOpsHubPaths.PasteElementNames.Body) ?? layout.Instantiate();
            root.name = LiveOpsHubPaths.PasteElementNames.Body;
            // Thiếu sheet riêng thì popover vẫn mở nhưng mất 320px, mất ô 96px, mất hàng nút dính phải — bỏ qua im lặng là một
            // bố cục sai không ai biết vì sao. Dùng lại đúng bộ cảnh báo một-lần-mỗi-đường-dẫn của cửa sổ riêng (lớp gốc).
            LiveOpsFeedbackStyleSheets.AddStyleSheets(root, PopoverOwnSheets, _layoutLoader);

            bool isImport = _mode == PasteRunningJsonMode.ImportIntoNewAsset;

            Label header = root.Q<Label>(LiveOpsHubPaths.PasteElementNames.Header);
            if (header != null)
            {
                header.text = isImport ? LiveOpsHubStrings.PasteImportPopoverTitle : LiveOpsHubStrings.PastePopoverTitle;
            }

            Label inputLabel = root.Q<Label>(LiveOpsHubPaths.PasteElementNames.InputLabel);
            if (inputLabel != null) inputLabel.text = LiveOpsHubStrings.PasteInputLabel;

            _inputField = root.Q<TextField>(LiveOpsHubPaths.PasteElementNames.Input);
            if (_inputField != null) _inputField.RegisterValueChangedCallback(changeEvent => RefreshStatus());

            _statusLabel = root.Q<Label>(LiveOpsHubPaths.PasteElementNames.Status);

            Label remoteKeyLine = root.Q<Label>(LiveOpsHubPaths.PasteElementNames.RemoteKeyLine);
            if (remoteKeyLine != null)
            {
                remoteKeyLine.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteRemoteKeyLineFormat, _remoteConfigKey);
                // Chế độ dán đã có asset nên key remote là việc của Tổng quan, không phải của cái ô dán này.
                remoteKeyLine.EnableInClassList(LiveOpsHubClassNames.PasteHidden, !isImport);
            }

            _modeGroup = root.Q<RadioButtonGroup>(LiveOpsHubPaths.PasteElementNames.Mode);
            if (_modeGroup != null)
            {
                _modeGroup.choices = new List<string> { LiveOpsHubStrings.PasteModeCompareOnly, LiveOpsHubStrings.PasteModeReplaceDraft };
                _modeGroup.SetValueWithoutNotify((int)PasteRunningJsonChoice.CompareOnly);
                // Chưa có asset thì không có nháp để thay: hiện hai lựa chọn lúc đó là mời người dùng bấm vào chỗ không có gì.
                _modeGroup.EnableInClassList(LiveOpsHubClassNames.PasteHidden, isImport);
            }

            _stampToggle = root.Q<Toggle>(LiveOpsHubPaths.PasteElementNames.StampToggle);
            // Mặc định BẬT: người dùng vừa nói "đây là thứ đang chạy", không ghi dấu là vứt lời đó (V-14 bước 4).
            if (_stampToggle != null) _stampToggle.SetValueWithoutNotify(true);

            Label stampLabel = root.Q<Label>(LiveOpsHubPaths.PasteElementNames.StampLabel);
            if (stampLabel != null) stampLabel.text = LiveOpsHubStrings.PasteStampToggleLabel;

            // Ẩn CẢ HÀNG chứ không riêng ô tích — ẩn mỗi ô tích thì câu ở lại một mình trong chế độ dán.
            VisualElement stampLine = root.Q(LiveOpsHubPaths.PasteElementNames.StampLine);
            if (stampLine != null) stampLine.EnableInClassList(LiveOpsHubClassNames.PasteHidden, !isImport);

            CancelButton = root.Q<Button>(LiveOpsHubPaths.PasteElementNames.Cancel);
            if (CancelButton != null)
            {
                CancelButton.text = LiveOpsHubStrings.PasteCancelButton;
                CancelButton.clicked += ClosePopover;
            }

            VisualElement footer = CancelButton != null ? CancelButton.parent : root;
            Button confirm = new Button(OnConfirmClicked)
            {
                name = LiveOpsHubPaths.PasteElementNames.Confirm,
                text = isImport ? LiveOpsHubStrings.PasteChooseSaveLocationButton : LiveOpsHubStrings.PasteConfirmButton,
            };
            confirm.AddToClassList(LiveOpsHubClassNames.Button);
            confirm.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _confirmSlot = new LiveOpsButtonSlot(confirm);
            footer.Add(_confirmSlot);

            RefreshStatus();
            return root;
        }

        /// <summary>
        /// Cú pháp trước, parser sau: người dùng cần vị trí dòng/ký tự để sửa, mà parser chỉ nói "không đọc được". Chỉ khi cú
        /// pháp sạch mới hỏi parser thật — đúng thứ tự của V-14 bước 2.
        /// </summary>
        private void RefreshStatus()
        {
            _readDocument = null;
            _readFormatVersion = 0;
            string pastedText = PastedText();

            if (pastedText.Length == 0)
            {
                SetStatus(string.Empty, false);
                SetConfirmEnabled(false, LiveOpsHubStrings.PasteNothingPastedReason);
                return;
            }

            LiveOpsJsonSyntaxLocator.SyntaxError syntaxError;
            if (LiveOpsJsonSyntaxLocator.TryFindFirstError(pastedText, out syntaxError))
            {
                string described = LiveOpsJsonSyntaxLocator.Describe(syntaxError);
                SetStatus(described, true);
                SetConfirmEnabled(false, described);
                return;
            }

            LiveEventCalendarDocumentParseResult parsed = _jsonReadBack.ReadBackDocument(pastedText);
            if (parsed == null || !parsed.IsReadable || parsed.Document == null)
            {
                string reason = parsed != null && parsed.ReadErrorText.Length > 0
                    ? parsed.ReadErrorText
                    : LiveOpsHubStrings.PasteNotReadableReason;
                SetStatus(reason, true);
                SetConfirmEnabled(false, reason);
                return;
            }

            _readDocument = parsed.Document;
            _readFormatVersion = parsed.FormatVersion;
            SetStatus(ReadableSummary(parsed.Document), false);
            SetConfirmEnabled(true, string.Empty);
        }

        private string ReadableSummary(LiveEventCalendarDocument document)
        {
            int undeclaredTypeCount = _countUndeclaredEventTypes == null ? 0 : _countUndeclaredEventTypes(document);
            string ruleCountText = _format.Integer(document.RecurringRules.Count);
            string eventCountText = _format.Integer(document.FixedEvents.Count);
            if (undeclaredTypeCount <= 0)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReadableFormat, ruleCountText, eventCountText);
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.PasteReadableWithUnknownTypesFormat,
                ruleCountText, eventCountText, _format.Integer(undeclaredTypeCount));
        }

        private void SetStatus(string text, bool isError)
        {
            if (_statusLabel == null) return;
            _statusLabel.text = text;
            _statusLabel.EnableInClassList(LiveOpsHubClassNames.TextBlocked, isError);
        }

        private void SetConfirmEnabled(bool enabled, string reason)
        {
            if (_confirmSlot == null) return;
            _confirmSlot.SetEnabledWithReason(enabled, reason);
        }

        private string PastedText()
        {
            return _inputField == null || _inputField.value == null ? string.Empty : _inputField.value;
        }

        private void OnConfirmClicked()
        {
            // Nút có thể bị gọi lại từ phím tắt hay test kể cả khi đang khoá: kiểm lại điều kiện thay vì tin vào trạng thái nút.
            if (_readDocument == null) return;
            PasteRunningJsonChoice choice = _mode == PasteRunningJsonMode.ImportIntoNewAsset || _modeGroup == null
                ? PasteRunningJsonChoice.CompareOnly
                : (PasteRunningJsonChoice)_modeGroup.value;
            bool stampAsPublished = _mode == PasteRunningJsonMode.ImportIntoNewAsset && _stampToggle != null && _stampToggle.value;
            SubmitCount++;
            PasteRunningJsonSubmission submission = new PasteRunningJsonSubmission(PastedText(), _readDocument, _readFormatVersion,
                choice, stampAsPublished);
            // Đóng TRƯỚC khi chạy luồng: luồng mở hộp xác nhận modal, mà popover còn nằm trên là hai cửa sổ chồng nhau.
            ClosePopover();
            _submit?.Invoke(submission);
        }
    }
}
