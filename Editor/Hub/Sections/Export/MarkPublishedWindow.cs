using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Kết quả hộp Đánh dấu đã đăng — Esc, nút Huỷ và đóng cửa sổ đều ra <see cref="Cancelled"/>.</summary>
    internal enum MarkPublishedResult
    {
        Cancelled = 0,
        Confirmed = 1,
    }

    /// <summary>
    /// Nội dung hộp Đánh dấu đã đăng — tách khỏi <see cref="MarkPublishedWindow"/> để test gửi phím và bấm nút mà không cần
    /// <c>ShowModalUtility</c> (lệnh đó chặn batchmode).
    /// <para>
    /// Mọi chữ và mọi bật/tắt đến từ <see cref="MarkPublishedModel.Evaluate(MarkPublishedInput)"/>: hộp chỉ gán. Nút
    /// "Copy JSON &lt;sha mới&gt;" gọi lại người mở hộp (section) để đi đúng đường Copy của màn — clipboard, ghi
    /// <c>lastExportedSha</c> và outcome đều là việc của phiên, không phải của một cửa sổ con.
    /// </para>
    /// </summary>
    internal sealed class MarkPublishedContent : VisualElement
    {
        internal const string RootElementName = "mark-published-root";
        internal const string ContentElementName = "mark-published-content";
        internal const string HeadingElementName = "mark-published-heading";
        internal const string BodyElementName = "mark-published-body";
        internal const string ShaRowElementName = "mark-published-sha-row";
        internal const string ShaMarkElementName = "mark-published-sha-mark";
        internal const string ShaTextElementName = "mark-published-sha-text";
        internal const string CopyRowElementName = "mark-published-copy-row";
        internal const string CopyButtonElementName = "mark-published-copy";
        internal const string ConfirmToggleElementName = "mark-published-confirm-toggle";
        internal const string NoteLabelElementName = "mark-published-note-label";
        internal const string NoteFieldElementName = "mark-published-note";
        internal const string MissingElementName = "mark-published-missing";
        internal const string CancelButtonElementName = "mark-published-cancel";
        internal const string ConfirmHostElementName = "mark-published-confirm-host";
        internal const string ConfirmButtonElementName = "mark-published-confirm";

        private static readonly string[] StyleSheetPaths =
        {
            LiveOpsHubPaths.ThemeUss, LiveOpsHubPaths.ComponentsUss, LiveOpsHubPaths.MarkPublishedWindowUss,
        };

        private const string CopyIconName = "Clipboard";
        private const int CopyIconSize = 14;

        private readonly Func<MarkPublishedInput> _copyCurrentJson;
        private readonly Label _heading;
        private readonly Label _body;
        private readonly LiveOpsStateMark _shaMark;
        private readonly Label _shaText;
        private readonly VisualElement _copyRow;
        private readonly Button _copyButton;
        private readonly Label _copyButtonLabel;
        private readonly Toggle _confirmToggle;
        private readonly Label _noteLabel;
        private readonly TextField _noteField;
        private readonly Button _cancelButton;
        private readonly Button _confirmButton;
        private readonly LiveOpsButtonSlot _confirmSlot;

        private MarkPublishedInput _input;

        public MarkPublishedContent(MarkPublishedInput input, Func<MarkPublishedInput> copyCurrentJson)
            : this(input, copyCurrentJson, new AssetDatabaseLiveOpsHubLayoutLoader())
        {
        }

        /// <param name="copyCurrentJson">Copy JSON nháp hiện tại qua đúng đường của màn, trả input đã cập nhật; null = không có nút copy.</param>
        /// <param name="layoutLoader">Test/chụp thay loader (V-16) — vd thiếu UXML.</param>
        internal MarkPublishedContent(MarkPublishedInput input, Func<MarkPublishedInput> copyCurrentJson, ILiveOpsHubLayoutLoader layoutLoader)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input), LiveOpsHubStrings.ExportGateErrorInputMissing);
            if (layoutLoader == null) throw new ArgumentNullException(nameof(layoutLoader));
            _copyCurrentJson = copyCurrentJson;

            name = RootElementName;
            AddToClassList(LiveOpsHubClassNames.Root);
            // Cửa sổ hộp là panel riêng: gắn lại token + sheet và class skin như root của cửa sổ hub.
            LiveOpsFeedbackStyleSheets.AddStyleSheets(this, StyleSheetPaths, layoutLoader);
            LiveOpsHubSkin.Attach(this);
            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            VisualTreeAsset layout = layoutLoader.LoadVisualTree(LiveOpsHubPaths.MarkPublishedWindowUxml);
            if (layout == null)
            {
                // Thiếu UXML: hộp vẫn mở nhưng chỉ còn đường huỷ — không dựng được câu hậu quả thì không mời ghi dấu.
                Label missingLayout = new Label(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ExportMissingLayoutFormat, LiveOpsHubPaths.MarkPublishedWindowUxml));
                missingLayout.AddToClassList(LiveOpsHubClassNames.ExportMarkBody);
                Add(missingLayout);
                _cancelButton = new Button(() => Complete(MarkPublishedResult.Cancelled))
                {
                    name = CancelButtonElementName,
                    text = LiveOpsHubStrings.ExportMarkCancelButton,
                };
                _cancelButton.AddToClassList(LiveOpsHubClassNames.Button);
                Add(_cancelButton);
                return;
            }

            VisualElement content = layout.Instantiate();
            content.style.flexGrow = 1f; // style-inline-allowed: 8
            Add(content);

            _heading = content.Q<Label>(HeadingElementName);
            _body = content.Q<Label>(BodyElementName);
            _shaMark = content.Q<LiveOpsStateMark>(ShaMarkElementName);
            _shaText = content.Q<Label>(ShaTextElementName);
            _copyRow = content.Q(CopyRowElementName);
            _copyButton = content.Q<Button>(CopyButtonElementName);
            _confirmToggle = content.Q<Toggle>(ConfirmToggleElementName);
            _noteLabel = content.Q<Label>(NoteLabelElementName);
            _noteField = content.Q<TextField>(NoteFieldElementName);
            Label missing = content.Q<Label>(MissingElementName);
            _cancelButton = content.Q<Button>(CancelButtonElementName);
            VisualElement confirmHost = content.Q(ConfirmHostElementName);

            // Button LÀ TextElement: `text` do chính nút vẽ trên cả hộp nội dung nên Image con nằm ĐÈ lên chữ. Nút copy của
            // biến thể "nháp đã đổi" mang icon clip [SD2 §3.11] nên chữ phải là Label con đặt SAU Image.
            _copyButton.AddToClassList(LiveOpsHubClassNames.ExportIconButton);
            Image copyIcon = LiveOpsHubIcons.CreateImage(CopyIconName, CopyIconSize);
            copyIcon.AddToClassList(LiveOpsHubClassNames.ExportIconButtonIcon);
            _copyButton.Add(copyIcon);
            _copyButtonLabel = new Label();
            _copyButtonLabel.AddToClassList(LiveOpsHubClassNames.ExportIconButtonLabel);
            _copyButton.Add(_copyButtonLabel);

            _confirmToggle.label = LiveOpsHubStrings.ExportMarkConfirmToggle;
            _noteLabel.text = LiveOpsHubStrings.ExportMarkNoteLabel;
            SetNotePlaceholder();
            _cancelButton.text = LiveOpsHubStrings.ExportMarkCancelButton;

            _confirmButton = new Button(OnConfirmClicked)
            {
                name = ConfirmButtonElementName,
                text = LiveOpsHubStrings.ExportMarkConfirmButton,
            };
            _confirmButton.AddToClassList(LiveOpsHubClassNames.Button);
            _confirmButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _confirmSlot = new LiveOpsButtonSlot(_confirmButton);
            confirmHost.Add(_confirmSlot);

            MissingLabel = missing;
            // Đầu vào đã mang tick + ghi chú (biến thể TRÁI của Hình 20, và mọi lần mở lại hộp với dữ liệu đã nhập) thì widget
            // phải mang đúng giá trị đó: Refresh đọc NGƯỢC từ widget, nên không gieo ở đây là hộp luôn hiện như chưa nhập gì.
            // SetValueWithoutNotify: gieo giá trị không phải là người dùng thao tác, và Refresh chạy ngay bên dưới.
            _confirmToggle.SetValueWithoutNotify(_input.ConfirmTicked);
            _noteField.SetValueWithoutNotify(_input.Note);
            _copyButton.clicked += OnCopyClicked;
            _cancelButton.clicked += OnCancelClicked;
            _confirmToggle.RegisterValueChangedCallback(changeEvent => Refresh());
            _noteField.RegisterValueChangedCallback(changeEvent => Refresh());

            Refresh();
        }

        /// <summary>Người dùng đã chốt: Confirmed (ghi dấu với <see cref="Note"/>) hay Cancelled.</summary>
        public event Action<MarkPublishedResult> Completed;

        public MarkPublishedInput Input => _input;
        public string Note => _noteField == null ? string.Empty : _noteField.value;
        public bool ConfirmTicked => _confirmToggle != null && _confirmToggle.value;

        internal Label MissingLabel { get; private set; }
        internal Label HeadingLabel => _heading;
        internal Label BodyLabel => _body;
        internal Label ShaLabel => _shaText;
        internal Button CopyButton => _copyButton;
        internal Button ConfirmButton => _confirmButton;
        internal Button CancelButton => _cancelButton;
        internal Toggle ConfirmToggle => _confirmToggle;
        internal TextField NoteField => _noteField;

        /// <summary>Trạng thái đang vẽ — test đọc thẳng thay vì suy ngược từ chữ trên màn.</summary>
        internal MarkPublishedState State { get; private set; }

        internal void Refresh()
        {
            if (_heading == null) return;
            MarkPublishedInput evaluated = _input.WithUserInput(ConfirmTicked, Note);
            MarkPublishedState state = MarkPublishedModel.Evaluate(evaluated);
            State = state;

            _heading.text = state.HeadingText;
            _body.text = state.BodyText;
            _shaText.text = state.ShaText;
            _shaMark.SetHealth(state.ShaState);

            bool hasCopy = state.HasCopyButton && _copyCurrentJson != null;
            _copyRow.EnableInClassList(LiveOpsHubClassNames.ExportHidden, !hasCopy);
            if (hasCopy) _copyButtonLabel.text = state.CopyButtonText;

            _confirmToggle.SetEnabled(state.ConfirmSectionEnabled);
            _noteLabel.SetEnabled(state.ConfirmSectionEnabled);
            _noteField.SetEnabled(state.ConfirmSectionEnabled);

            MissingLabel.text = state.MissingText;
            MissingLabel.EnableInClassList(LiveOpsHubClassNames.ExportHidden, state.MissingText.Length == 0);
            // Câu "Còn thiếu…" in ĐÚNG MỘT chỗ — nhãn trái hàng nút [SD2 §3.11]. Slot vẫn nhận câu đó để mang tooltip (nút
            // disabled không nhận hover, R-16), nhưng nhãn lý do của slot bị ẩn: in hai lần làm hàng nút tràn và cắt mất
            // chính nút "Ghi dấu đã đăng".
            _confirmSlot.SetEnabledWithReason(state.CanMark, state.MissingText);
            _confirmSlot.ReasonLabel.AddToClassList(LiveOpsHubClassNames.ExportReasonTooltipOnly);
        }

        private void SetNotePlaceholder()
        {
            // Label placeholder phủ TextField là chỗ 8 của [FD §2.14]; 2022.3 chưa có thuộc tính placeholder trên TextField.
            Label placeholder = new Label(LiveOpsHubStrings.ExportMarkNotePlaceholder);
            placeholder.AddToClassList(LiveOpsHubClassNames.Placeholder);
            placeholder.pickingMode = PickingMode.Ignore;
            _noteField.Add(placeholder);
            // Gieo sẵn ghi chú không phát ValueChanged nên trạng thái đầu của placeholder phải tự tính, không chờ callback.
            placeholder.EnableInClassList(LiveOpsHubClassNames.PlaceholderHidden, _input.Note.Length > 0);
            _noteField.RegisterValueChangedCallback(changeEvent =>
                placeholder.EnableInClassList(LiveOpsHubClassNames.PlaceholderHidden, changeEvent.newValue.Length > 0));
        }

        private void OnCopyClicked()
        {
            if (_copyCurrentJson == null) return;
            MarkPublishedInput copied = _copyCurrentJson();
            if (copied != null) _input = copied;
            Refresh();
        }

        private void OnConfirmClicked()
        {
            if (State == null || !State.CanMark) return;
            Complete(MarkPublishedResult.Confirmed);
        }

        private void OnCancelClicked()
        {
            Complete(MarkPublishedResult.Cancelled);
        }

        private void OnKeyDown(KeyDownEvent keyDownEvent)
        {
            // Esc = huỷ, luôn an toàn. Enter KHÔNG ghi dấu: ô ghi chú là ô nhiều dòng, Enter ở đó là xuống dòng
            // (SPIKE-B SP-7b — phím đơn bỏ qua khi focus ở ô nhập chữ).
            if (keyDownEvent.keyCode != KeyCode.Escape) return;
            keyDownEvent.StopPropagation();
            Complete(MarkPublishedResult.Cancelled);
        }

        private void Complete(MarkPublishedResult result)
        {
            Action<MarkPublishedResult> handler = Completed;
            if (handler != null) handler(result);
        }
    }

    /// <summary>
    /// Hộp Đánh dấu đã đăng [SD2 §3.11]: cửa sổ UXML riêng 410×330 mở bằng <c>ShowModalUtility</c>, KHÔNG phải cấp 2 của
    /// <see cref="LiveOpsConfirmWindow"/> — đây là chỗ D5 hiện ra trên màn hình, dấu "đã đăng" là việc người dùng tự khai.
    /// </summary>
    internal sealed class MarkPublishedWindow : EditorWindow
    {
        internal const float Width = 410f;
        internal const float Height = 330f;

        private MarkPublishedContent _content;

        internal MarkPublishedContent Content => _content;
        internal MarkPublishedResult Result { get; private set; } = MarkPublishedResult.Cancelled;

        /// <summary>Ghi chú người dùng gõ khi bấm "Ghi dấu đã đăng"; "" khi huỷ.</summary>
        internal string Note { get; private set; } = string.Empty;

        /// <summary>Mở modal và chờ; trả Cancelled khi Esc / Huỷ / đóng cửa sổ. Không gọi trong test (chặn batchmode).</summary>
        public static MarkPublishedWindow Show(MarkPublishedInput input, Func<MarkPublishedInput> copyCurrentJson)
        {
            MarkPublishedWindow window = Create(input, copyCurrentJson, null);
            window.ShowModalUtility();
            // Cửa sổ đã đóng (Unity object == null) nhưng instance C# vẫn giữ Result và Note đã chốt.
            return window;
        }

        /// <summary>Mở KHÔNG modal (test UI + kịch bản chụp h20a/b/c): cùng nội dung, cùng kích thước. Người gọi đóng cửa sổ.</summary>
        internal static MarkPublishedWindow OpenForTest(MarkPublishedInput input, Func<MarkPublishedInput> copyCurrentJson,
            ILiveOpsHubLayoutLoader layoutLoader = null)
        {
            MarkPublishedWindow window = Create(input, copyCurrentJson, layoutLoader);
            window.Show();
            // SP-16: kích thước đặt SAU Show mới giữ (đặt trước bị kẹp).
            window.position = new Rect(0f, 0f, Width, Height);
            window.Focus();
            return window;
        }

        private static MarkPublishedWindow Create(MarkPublishedInput input, Func<MarkPublishedInput> copyCurrentJson,
            ILiveOpsHubLayoutLoader layoutLoader)
        {
            if (input == null) throw new ArgumentNullException(nameof(input), LiveOpsHubStrings.ExportGateErrorInputMissing);
            MarkPublishedWindow window = CreateInstance<MarkPublishedWindow>();
            window.titleContent = new GUIContent(LiveOpsHubStrings.ExportMarkWindowTitle);
            Vector2 size = new Vector2(Width, Height);
            window.minSize = size;
            window.maxSize = size;
            window.CenterOnMainWindow(size);
            window._content = layoutLoader == null
                ? new MarkPublishedContent(input, copyCurrentJson)
                : new MarkPublishedContent(input, copyCurrentJson, layoutLoader);
            window._content.Completed += window.OnCompleted;
            window.rootVisualElement.Add(window._content);
            return window;
        }

        private void CenterOnMainWindow(Vector2 size)
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            if (main.width <= 0f || main.height <= 0f) return;
            position = new Rect(main.x + (main.width - size.x) * 0.5f, main.y + (main.height - size.y) * 0.5f, size.x, size.y);
        }

        private void OnCompleted(MarkPublishedResult result)
        {
            Result = result;
            Note = result == MarkPublishedResult.Confirmed ? _content.Note : string.Empty;
            // Đóng ngay (không delayCall): vòng modal của ShowModalUtility không chắc chạy delayCall.
            Close();
        }
    }
}
