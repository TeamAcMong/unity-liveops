using System;
using System.Globalization;
using DreamTech.LiveOps.Unity;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Foldout "JSON của luật này" ở cuối form Luật lặp (mục 7.4, [SD1 §4.1]) — bản SỬA ĐƯỢC của W5, thay bản chỉ đọc + Copy
    /// của W4 (mục 12 I-6). Người dùng gõ/dán một object luật lặp; foldout kiểm ngay hai tầng và chỉ bật "Áp" khi cả hai qua:
    /// <list type="number">
    /// <item><see cref="LiveOpsJsonSyntaxLocator"/> — cú pháp, cho ra câu có vị trí "Dòng 3, ký tự 18: thiếu dấu phẩy" (PD-13).</item>
    /// <item><see cref="ILiveOpsHubJsonReadBack.ReadBackDocument"/> — ĐÚNG bước đọc của parser game, nên "hub nói đọc được"
    /// và "game đọc được" không bao giờ lệch nhau.</item>
    /// </list>
    /// <para>
    /// Foldout KHÔNG tự ghi: bấm "Áp" chỉ phát <see cref="ApplyRequested"/> kèm luật đọc được; section mới là nơi hỏi policy
    /// rồi ghi thẳng hay giữ nháp tại ô (cùng đường với mọi thay đổi khác của form, xem <c>RecurringRulesSection</c>).
    /// </para>
    /// <para>
    /// Kiểm cú pháp chạy trên chuỗi NGƯỜI DÙNG gõ, không trên chuỗi đã bọc: dòng/ký tự phải trỏ đúng chỗ trong ô. Chỉ khi cú
    /// pháp đã sạch mới bọc thành một tài liệu đủ (<c>version</c> + <c>recurring</c> + <c>events</c>) để mượn parser game —
    /// lúc đó số dòng không còn dùng tới nên phần bọc không làm sai câu lỗi nào.
    /// </para>
    /// </summary>
    internal sealed class RecurringRuleJsonFoldout : Foldout
    {
        internal const string ElementName = "recurring-json-foldout";
        internal const string EditorElementName = "recurring-json-text";
        internal const string ErrorElementName = "recurring-json-error";
        internal const string ApplyElementName = "recurring-json-apply";
        internal const string CopyElementName = "recurring-json-copy";

        /// <summary>Bọc object luật thành một tài liệu định dạng 2 đủ mảng để <c>ParseDocument</c> nhận ra và đọc.</summary>
        private const string DocumentPrefix = "{\"version\":2,\"recurring\":[";

        private const string DocumentSuffix = "],\"events\":[]}";

        private readonly TextField _editor = new TextField { name = EditorElementName, multiline = true };
        private readonly Label _errorLabel = new Label { name = ErrorElementName };
        private readonly LiveOpsButtonSlot _applySlot;

        private string _eventType = string.Empty;
        private string _writtenJson = string.Empty;
        private RecurringLiveEventRule _candidate;
        private bool _isEdited;
        private ILiveOpsHubJsonReadBack _jsonReadBack = new GameParserLiveOpsHubJsonReadBack();

        internal RecurringRuleJsonFoldout()
        {
            name = ElementName;
            text = LiveOpsHubStrings.RecurringJsonFoldoutLabel;
            value = false;
            AddToClassList(LiveOpsHubClassNames.RecurringJson);

            _editor.AddToClassList(LiveOpsHubClassNames.Mono);
            _editor.AddToClassList(LiveOpsHubClassNames.RecurringJsonEditor);
            Add(_editor);

            _errorLabel.AddToClassList(LiveOpsHubClassNames.RecurringJsonError);
            _errorLabel.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            Add(_errorLabel);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.RecurringJsonActions);
            Button applyButton = new Button(RaiseApply) { name = ApplyElementName, text = LiveOpsHubStrings.RecurringJsonApplyButton };
            applyButton.AddToClassList(LiveOpsHubClassNames.Button);
            applyButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _applySlot = new LiveOpsButtonSlot(applyButton);
            actions.Add(_applySlot);
            Button copyButton = new Button(RaiseCopy) { name = CopyElementName, text = LiveOpsHubStrings.RecurringJsonCopyButton };
            copyButton.AddToClassList(LiveOpsHubClassNames.Button);
            actions.Add(copyButton);
            Add(actions);

            // Không isDelayed: người sửa JSON cần thấy câu lỗi trong lúc gõ, không phải sau khi rời ô.
            _editor.RegisterValueChangedCallback(changeEvent => OnEditorTextChanged());
            Validate();
        }

        /// <summary>Bấm "Áp" với JSON đọc được — kèm luật đã đọc; section quyết ghi thẳng hay giữ nháp.</summary>
        public event Action<RecurringLiveEventRule> ApplyRequested;

        public event Action CopyRequested;

        /// <summary>
        /// Đường đọc lại của phiên (<c>Services.JsonReadBack</c>) — section gắn một lần lúc dựng để foldout đi đúng adapter
        /// mà kịch bản chụp / test đang thay (V-16). Mặc định là parser thật của game, nên control dựng riêng vẫn đọc đúng
        /// thứ game đọc; gán null quay về mặc định thay vì để một ô không kiểm được gì.
        /// </summary>
        internal ILiveOpsHubJsonReadBack JsonReadBack
        {
            get { return _jsonReadBack; }
            set { _jsonReadBack = value ?? new GameParserLiveOpsHubJsonReadBack(); }
        }

        internal TextField Editor => _editor;
        internal Label ErrorLabel => _errorLabel;
        internal LiveOpsButtonSlot ApplySlot => _applySlot;

        /// <summary>Câu lỗi đang hiện; "" khi JSON đọc được.</summary>
        internal string ErrorText => _errorLabel.text;

        /// <summary>Luật đọc được từ ô, hoặc null khi ô đang lỗi / chưa đổi gì.</summary>
        internal RecurringLiveEventRule Candidate => _candidate;

        /// <summary>
        /// Vẽ lại theo luật đang chọn. Khi người dùng ĐANG sửa dở thì không đè chữ trong ô: mỗi lần phiên phát
        /// <c>DocumentChanged</c> là một lần Bind, đè thì JSON đang gõ biến mất giữa chừng. Đổi sang luật khác (loại khác)
        /// thì bỏ hẳn phần đang gõ — nó là JSON của luật cũ.
        /// </summary>
        public void Bind(string eventType, string writtenJson)
        {
            string nextEventType = eventType ?? string.Empty;
            if (!string.Equals(nextEventType, _eventType, StringComparison.Ordinal)) _isEdited = false;
            _eventType = nextEventType;
            _writtenJson = writtenJson ?? string.Empty;
            if (!_isEdited) _editor.SetValueWithoutNotify(_writtenJson);
            Validate();
        }

        private void OnEditorTextChanged()
        {
            _isEdited = true;
            Validate();
        }

        private void Validate()
        {
            _candidate = null;
            string text = _editor.value ?? string.Empty;
            if (string.Equals(text, _writtenJson, StringComparison.Ordinal))
            {
                ShowBlocked(LiveOpsHubStrings.RecurringJsonUnchangedReason, showAsError: false);
                return;
            }

            LiveOpsJsonSyntaxLocator.SyntaxError syntaxError;
            if (LiveOpsJsonSyntaxLocator.TryFindFirstError(text, out syntaxError))
            {
                ShowBlocked(LiveOpsJsonSyntaxLocator.Describe(syntaxError), showAsError: true);
                return;
            }

            LiveEventCalendarDocumentParseResult result = _jsonReadBack.ReadBackDocument(DocumentPrefix + text + DocumentSuffix);
            if (!result.IsReadable)
            {
                ShowBlocked(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringJsonUnreadableFormat,
                    result.ReadErrorText), showAsError: true);
                return;
            }
            if (result.Document.RecurringRules.Count != 1)
            {
                ShowBlocked(LiveOpsHubStrings.RecurringJsonNotOneRuleReason, showAsError: true);
                return;
            }

            RecurringLiveEventRule parsed = result.Document.RecurringRules[0];
            if (!string.Equals(parsed.EventType, _eventType, StringComparison.Ordinal))
            {
                ShowBlocked(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringJsonTypeLockedFormat,
                    _eventType), showAsError: true);
                return;
            }

            _candidate = parsed;
            _errorLabel.text = string.Empty;
            _errorLabel.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, true);
            _applySlot.SetEnabledWithReason(true, string.Empty);
        }

        /// <summary>
        /// Khoá "Áp" kèm lý do. Lý do đi cả vào nhãn cạnh nút (<c>LiveOpsButtonSlot</c>, SP-3) lẫn dòng lỗi khi đó là LỖI của
        /// JSON; "chưa đổi gì" không phải lỗi nên không tô dòng đỏ dưới ô — chỉ nút nói vì sao nó đứng yên.
        /// </summary>
        private void ShowBlocked(string reason, bool showAsError)
        {
            _errorLabel.text = showAsError ? reason : string.Empty;
            _errorLabel.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !showAsError);
            _applySlot.SetEnabledWithReason(false, reason);
        }

        private void RaiseApply()
        {
            if (_candidate == null || ApplyRequested == null) return;
            RecurringLiveEventRule candidate = _candidate;
            // Bỏ cờ "đang sửa dở" TRƯỚC khi phát: lần Bind ngay sau đó đưa ô về JSON của luật mà form đang hiện — kể cả khi
            // hộp xác nhận bị huỷ, vì lúc đó nháp vẫn còn và JSON của nháp chính là chữ người dùng vừa gõ.
            _isEdited = false;
            ApplyRequested(candidate);
        }

        private void RaiseCopy()
        {
            if (CopyRequested != null) CopyRequested();
        }
    }
}
