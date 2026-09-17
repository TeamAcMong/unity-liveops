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
        private string _draftLockReason = string.Empty;
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
            // Nút ĐẦU của hàng: .liveops-hub-button có margin-left 4px, không bỏ thì hàng nút thụt vào 4px so với mép ô JSON
            // (W4 bỏ đúng chỗ này trên nút Copy, lúc Copy còn đứng đầu).
            applyButton.AddToClassList(LiveOpsHubClassNames.ButtonFirst);
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

        /// <summary>
        /// (Q-W4-4, user duyệt 16/9) Form báo xuống: đang có một ô giữ nháp chưa ghi, kèm lý do đã dựng sẵn thành chữ ("" =
        /// hết nháp). Ô JSON là lối sửa thứ NĂM của cùng một luật, nên nó phải đóng cùng lỗ hổng mà Q-W4-4 bịt ở bốn ô kia:
        /// bấm "Áp" trong lúc còn nháp sẽ dựng nháp MỚI đè nháp cũ và cái người dùng vừa gõ ở ô kia biến mất không dấu vết.
        /// Ô nhập vẫn mở để còn đọc/sao chép JSON — chỉ nút ghi bị khoá, và lý do in thành chữ cạnh nút (SPIKE-B SP-3).
        /// </summary>
        internal void SetLockedByDraft(string reason)
        {
            string nextReason = reason ?? string.Empty;
            if (string.Equals(nextReason, _draftLockReason, StringComparison.Ordinal)) return;
            _draftLockReason = nextReason;
            Validate();
        }

        private void Validate()
        {
            _candidate = null;
            string text = _editor.value ?? string.Empty;
            // Nháp đang giữ ở một ô khác thì khoá TRƯỚC mọi kiểm khác: dù JSON có đọc được thì áp vào lúc này cũng nuốt nháp.
            if (_draftLockReason.Length > 0)
            {
                ShowBlocked(_draftLockReason, showAsError: false);
                return;
            }
            if (string.Equals(text, _writtenJson, StringComparison.Ordinal))
            {
                ShowBlocked(LiveOpsHubStrings.RecurringJsonUnchangedReason, showAsError: false);
                return;
            }

            LiveOpsJsonSyntaxLocator.SyntaxError syntaxError;
            // allowMultipleRoots: ô này được bọc vào mảng "recurring" trước khi đưa cho parser, nên hai luật cách nhau bằng
            // dấu phẩy KHÔNG phải lỗi cú pháp — nó phải ra câu thân thiện "nhận đúng một object luật lặp" ở dưới.
            if (LiveOpsJsonSyntaxLocator.TryFindFirstError(text, true, out syntaxError))
            {
                // Q-W5-3: đây là nhánh mà ảnh ghim h14e đi qua — câu của bộ dò ("Dòng 3, ký tự 18: thiếu dấu phẩy") ở lại dòng
                // lỗi, còn cạnh nút chỉ nói "JSON chưa đọc được". Cú pháp hỏng cũng là một kiểu không đọc được, nên dùng chung
                // câu ngắn với nhánh parser bên dưới.
                ShowBlocked(LiveOpsJsonSyntaxLocator.Describe(syntaxError), showAsError: true,
                    LiveOpsHubStrings.RecurringJsonUnreadableShortReason);
                return;
            }

            // Cú pháp sạch nhưng gốc không phải object (mảng, số, chuỗi…): bọc vào mảng rồi mượn parser game sẽ ra câu trần
            // của JsonUtility, trong khi câu đúng — và đã hứa sẵn trong RecurringJsonNotOneRuleReason — là "không phải mảng".
            if (!StartsWithObject(text))
            {
                ShowBlocked(LiveOpsHubStrings.RecurringJsonNotOneRuleReason, showAsError: true,
                    LiveOpsHubStrings.RecurringJsonNotOneRuleShortReason);
                return;
            }

            LiveEventCalendarDocumentParseResult result = _jsonReadBack.ReadBackDocument(DocumentPrefix + text + DocumentSuffix);
            if (!result.IsReadable)
            {
                // Q-W5-3: dòng lỗi dưới ô giữ CÂU ĐẦY ĐỦ (có nguyên văn câu của parser — người sửa cần đúng câu game sẽ gặp),
                // còn chỗ cạnh nút "Áp" chỉ đủ một vế ngắn. Hai chỗ, hai câu — không in hai lần cùng một câu trong một khung nhìn.
                ShowBlocked(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringJsonUnreadableFormat,
                    result.ReadErrorText), showAsError: true, LiveOpsHubStrings.RecurringJsonUnreadableShortReason);
                return;
            }
            if (result.Document.RecurringRules.Count != 1)
            {
                ShowBlocked(LiveOpsHubStrings.RecurringJsonNotOneRuleReason, showAsError: true,
                    LiveOpsHubStrings.RecurringJsonNotOneRuleShortReason);
                return;
            }

            RecurringLiveEventRule parsed = result.Document.RecurringRules[0];
            if (!string.Equals(parsed.EventType, _eventType, StringComparison.Ordinal))
            {
                ShowBlocked(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringJsonTypeLockedFormat,
                    _eventType), showAsError: true, LiveOpsHubStrings.RecurringJsonTypeLockedShortReason);
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
        /// <para>
        /// Bản hai tham số này chỉ còn cho hai ca KHÔNG phải lỗi (khoá vì nháp, chưa đổi gì): ở đó dòng lỗi ẩn nên câu chỉ in
        /// một chỗ. Mọi nhánh LỖI phải truyền câu ngắn riêng (Q-W5-3) — nếu không, cùng một câu in hai lần trong một khung nhìn.
        /// </para>
        /// </summary>
        private void ShowBlocked(string reason, bool showAsError)
        {
            ShowBlocked(reason, showAsError, string.Empty);
        }

        /// <param name="shortReason">
        /// Câu ngắn riêng cho nhãn cạnh nút (Q-W5-3); rỗng = nhãn dùng luôn <paramref name="reason"/> như các ca khác.
        /// </param>
        private void ShowBlocked(string reason, bool showAsError, string shortReason)
        {
            _errorLabel.text = showAsError ? reason : string.Empty;
            _errorLabel.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !showAsError);
            _applySlot.SetEnabledWithReason(false, shortReason.Length > 0 ? shortReason : reason);
        }

        /// <summary>Ký tự đáng kể đầu tiên có phải '{' — chuỗi đã qua bộ dò cú pháp nên không rỗng và không chỉ có khoảng trắng.</summary>
        private static bool StartsWithObject(string text)
        {
            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                if (character == ' ' || character == '\t' || character == '\n' || character == '\r') continue;
                return character == '{';
            }
            return false;
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
