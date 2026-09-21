using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Form của một luật lặp ([SD1 §4.1–4.2], max-width 640): câu token, sáu field, thanh chu kỳ, card "n đợt kế tiếp" và
    /// foldout JSON. Form KHÔNG tự ghi gì: mỗi lần một field commit, nó phát <see cref="ValueChangeRequested"/> kèm luật ứng
    /// viên, còn section mới là nơi hỏi policy rồi quyết định ghi thẳng hay giữ nháp tại ô.
    /// <para>
    /// Field dùng <c>isDelayed</c> nên chỉ commit khi Enter hoặc rời ô: gõ từng ký tự mà đã hỏi "có đổi id đợt đang chạy
    /// không" thì hộp xác nhận bật lên giữa lúc đang gõ.
    /// </para>
    /// <para>
    /// (Q-W4-4) Khi một trong bốn ô sửa được đang giữ nháp thì ba ô còn lại bị khoá kèm lý do in thành chữ — nháp chỉ sống
    /// ở một ô, nên mở cả bốn là mời người dùng đánh mất thứ mình vừa gõ.
    /// </para>
    /// </summary>
    internal sealed class RecurringRuleForm : VisualElement
    {
        internal const string SentenceElementName = "recurring-sentence";
        internal const string PresetFieldElementName = "recurring-preset-field";
        internal const string TypeFieldElementName = "recurring-type-field";
        internal const string PrefixFieldElementName = "recurring-prefix-field";
        internal const string AnchorFieldElementName = "recurring-anchor-field";
        internal const string PeriodFieldElementName = "recurring-period-field";
        internal const string ActiveFieldElementName = "recurring-active-field";
        internal const string CycleBarElementName = "recurring-cycle-bar";
        internal const string CycleTextElementName = "recurring-cycle-text";
        internal const string DraftBlockElementName = "recurring-draft-block";
        internal const string DraftNoticeElementName = "recurring-draft-notice";
        internal const string DraftHelpBoxElementName = "recurring-draft-helpbox";
        internal const string DraftCancelElementName = "recurring-draft-cancel";
        internal const string DraftWriteElementName = "recurring-draft-write";
        internal const string AfterWriteElementName = "recurring-after-write";
        internal const string AfterWriteRevertElementName = "recurring-after-write-revert";
        internal const string AfterWriteLinkElementName = "recurring-after-write-link";
        internal const string AnchorNoticeElementName = "recurring-anchor-notice";
        internal const string DeleteElementName = "recurring-delete";

        private readonly RecurringRuleSentence _sentence = new RecurringRuleSentence { name = SentenceElementName };
        private readonly DropdownField _presetField = new DropdownField { name = PresetFieldElementName };
        private readonly DropdownField _typeField = new DropdownField { name = TypeFieldElementName };
        private readonly TextField _prefixField = new TextField { name = PrefixFieldElementName, isDelayed = true };
        private readonly LiveOpsUtcDateTimeField _anchorField = new LiveOpsUtcDateTimeField { name = AnchorFieldElementName };
        private readonly IntegerField _periodField = new IntegerField { name = PeriodFieldElementName, isDelayed = true };
        private readonly IntegerField _activeField = new IntegerField { name = ActiveFieldElementName, isDelayed = true };
        private readonly LiveOpsRuleCycleBar _cycleBar = new LiveOpsRuleCycleBar { name = CycleBarElementName };
        private readonly Label _cycleText = new Label { name = CycleTextElementName };
        private readonly RecurringNextOccurrencesTable _occurrences = new RecurringNextOccurrencesTable();
        private readonly Dictionary<string, FieldSlot> _slots = new Dictionary<string, FieldSlot>(StringComparer.Ordinal);

        /// <summary>Thứ tự ô đúng như trên màn: Dictionary không hứa thứ tự, mà câu khoá phải đứng ở ô ĐẦU của nhóm bị khoá.</summary>
        private readonly List<string> _slotOrder = new List<string>();

        private readonly VisualElement _draftBlock;
        private readonly Label _draftNotice;
        private readonly HelpBox _draftHelpBox;
        private readonly Button _draftWriteButton;
        private readonly Button _draftCancelButton;
        private readonly VisualElement _afterWriteBlock;
        private readonly HelpBox _afterWriteHelpBox;
        private readonly Button _afterWriteRevertButton;
        private readonly HelpBox _anchorNotice;
        private readonly RecurringRuleJsonFoldout _jsonFoldout = new RecurringRuleJsonFoldout();

        private RecurringRuleModel _model = RecurringRuleModel.Empty(string.Empty);
        private RecurringPrefixDraft _draft = RecurringPrefixDraft.None;
        private IReadOnlyList<string> _presetNames = Array.Empty<string>();
        private string _revertPrefix = string.Empty;

        internal RecurringRuleForm()
        {
            AddToClassList(LiveOpsHubClassNames.RecurringForm);

            Add(_sentence);
            _sentence.TokenClicked += FocusField;

            AddFieldRow(string.Empty, LiveOpsHubStrings.RecurringPresetLabel, _presetField, false);
            AddFieldRow(string.Empty, LiveOpsHubStrings.RecurringEventTypeLabel, _typeField, false);
            AddFieldRow(RecurringRuleFields.IdPrefix, LiveOpsHubStrings.RecurringIdPrefixLabel, _prefixField, true);
            AddFieldRow(RecurringRuleFields.Anchor, LiveOpsHubStrings.RecurringAnchorLabel, _anchorField, true);
            AddFieldRow(RecurringRuleFields.PeriodHours, LiveOpsHubStrings.RecurringPeriodHoursLabel, _periodField, true);
            AddFieldRow(RecurringRuleFields.ActiveHours, LiveOpsHubStrings.RecurringActiveHoursLabel, _activeField, true);

            _prefixField.AddToClassList(LiveOpsHubClassNames.Mono);
            // Dòng giờ máy của chính ô sẽ dùng lệch mặc định 0 (ô không biết múi giờ của phiên) và in "00:00 … giờ máy" sai.
            // Chữ phụ đúng — kèm thứ trong tuần — do model dựng bằng LiveOpsHubFormat của services ([SD1 §4.1]).
            _anchorField.ShowDeviceTimeLine = false;
            _periodField.AddToClassList(LiveOpsHubClassNames.RecurringFieldNumber);
            _activeField.AddToClassList(LiveOpsHubClassNames.RecurringFieldNumber);
            // Loại khoá sau khi tạo (mục 7.4): đổi loại là một luật khác hẳn, nên đường duy nhất là "Thêm luật".
            _typeField.SetEnabled(false);
            _typeField.tooltip = LiveOpsHubStrings.RecurringEventTypeLockedTooltip;

            _anchorNotice = new HelpBox(string.Empty, HelpBoxMessageType.Info) { name = AnchorNoticeElementName };
            SlotOf(RecurringRuleFields.Anchor).NoticeHost.Add(_anchorNotice);

            _draftNotice = new Label { name = DraftNoticeElementName };
            _draftHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning) { name = DraftHelpBoxElementName };
            _draftWriteButton = new Button(RaiseDraftWrite) { name = DraftWriteElementName };
            _draftWriteButton.AddToClassList(LiveOpsHubClassNames.Button);
            _draftWriteButton.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            _draftCancelButton = new Button(RaiseDraftCancel) { name = DraftCancelElementName, text = LiveOpsHubStrings.RecurringDraftCancelButton };
            _draftCancelButton.AddToClassList(LiveOpsHubClassNames.Button);
            _draftCancelButton.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _draftCancelButton.AddToClassList(LiveOpsHubClassNames.ButtonFirst);

            _draftBlock = new VisualElement { name = DraftBlockElementName };
            _draftBlock.AddToClassList(LiveOpsHubClassNames.RecurringDraftBlock);
            _draftNotice.AddToClassList(LiveOpsHubClassNames.RecurringDraftNotice);
            _draftBlock.Add(_draftNotice);
            _draftBlock.Add(_draftHelpBox);
            VisualElement draftActions = new VisualElement();
            draftActions.AddToClassList(LiveOpsHubClassNames.RecurringDraftActions);
            draftActions.Add(_draftCancelButton);
            draftActions.Add(_draftWriteButton);
            _draftBlock.Add(draftActions);

            _afterWriteHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _afterWriteRevertButton = new Button(RaiseRevertPrefix) { name = AfterWriteRevertElementName };
            _afterWriteRevertButton.AddToClassList(LiveOpsHubClassNames.Button);
            _afterWriteRevertButton.AddToClassList(LiveOpsHubClassNames.ButtonFirst);
            Button decideLink = new Button(RaiseDecideInValidation)
            {
                name = AfterWriteLinkElementName,
                text = LiveOpsHubStrings.RecurringDecideInValidationLink,
            };
            decideLink.AddToClassList(LiveOpsHubClassNames.RecurringLink);
            _afterWriteBlock = new VisualElement { name = AfterWriteElementName };
            _afterWriteBlock.AddToClassList(LiveOpsHubClassNames.RecurringAfterWrite);
            _afterWriteBlock.Add(_afterWriteHelpBox);
            VisualElement afterWriteActions = new VisualElement();
            afterWriteActions.AddToClassList(LiveOpsHubClassNames.RecurringAfterWriteActions);
            afterWriteActions.Add(_afterWriteRevertButton);
            afterWriteActions.Add(decideLink);
            _afterWriteBlock.Add(afterWriteActions);
            SlotOf(RecurringRuleFields.IdPrefix).NoticeHost.Add(_afterWriteBlock);

            VisualElement cycleRow = new VisualElement();
            cycleRow.AddToClassList(LiveOpsHubClassNames.RecurringCycleRow);
            cycleRow.Add(_cycleBar);
            _cycleText.AddToClassList(LiveOpsHubClassNames.RecurringCycleText);
            cycleRow.Add(_cycleText);
            Add(cycleRow);

            _occurrences.AddMoreRequested += RaiseAddMoreOccurrences;
            Add(_occurrences);

            _jsonFoldout.CopyRequested += RaiseCopyJson;
            _jsonFoldout.ApplyRequested += RaiseApplyJson;
            Add(_jsonFoldout);

            Button deleteButton = new Button(RaiseDelete) { name = DeleteElementName, text = LiveOpsHubStrings.RecurringDeleteRuleButton };
            deleteButton.AddToClassList(LiveOpsHubClassNames.Button);
            deleteButton.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            deleteButton.AddToClassList(LiveOpsHubClassNames.ButtonFirst);
            // (J3-01) Form luật là hộp xếp DỌC, nên không có class này thì nút xoá lấy trọn bề ngang pane (đo được 637px ở
            // 1280 và 504px ở 820) — hành động KHÔNG hoàn tác được mà lại là phần tử rộng nhất màn.
            deleteButton.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
            Add(deleteButton);

            RegisterFieldCallbacks();
            RegisterCallback<PointerDownEvent>(OnPointerDownInsideForm, TrickleDown.TrickleDown);
        }

        /// <summary>Một field vừa commit: luật ứng viên đã áp giá trị mới. Section quyết định ghi hay giữ nháp.</summary>
        public event Action<string, RecurringLiveEventRule> ValueChangeRequested;

        public event Action<int> PresetSelected;
        public event Action DraftCancelRequested;
        public event Action DraftWriteRequested;
        public event Action RevertPrefixRequested;
        public event Action DecideInValidationRequested;
        public event Action AddMoreOccurrencesRequested;
        public event Action CopyJsonRequested;

        /// <summary>Bấm "Áp" trong foldout JSON với một luật đọc được — cùng đích với <see cref="ValueChangeRequested"/>.</summary>
        public event Action<RecurringLiveEventRule> ApplyJsonRequested;
        public event Action DeleteRequested;

        internal RecurringNextOccurrencesTable Occurrences => _occurrences;
        internal RecurringRuleSentence Sentence => _sentence;
        internal TextField PrefixField => _prefixField;
        internal IntegerField PeriodField => _periodField;
        internal IntegerField ActiveField => _activeField;
        internal LiveOpsUtcDateTimeField AnchorField => _anchorField;
        internal DropdownField PresetField => _presetField;
        internal RecurringRuleJsonFoldout JsonFoldout => _jsonFoldout;
        internal VisualElement DraftBlock => _draftBlock;
        internal VisualElement AfterWriteBlock => _afterWriteBlock;

        /// <summary>Danh sách tên mẫu cho dropdown "Mẫu" — luôn có mục cuối "Tùy chỉnh…".</summary>
        public void SetPresetNames(IReadOnlyList<string> presetNames)
        {
            _presetNames = presetNames ?? Array.Empty<string>();
            List<string> choices = new List<string>(_presetNames.Count + 1);
            for (int index = 0; index < _presetNames.Count; index++) choices.Add(_presetNames[index]);
            choices.Add(LiveOpsHubStrings.RecurringPresetCustom);
            _presetField.choices = choices;
        }

        /// <summary>Vẽ lại toàn bộ form theo model + nháp đang mở; <paramref name="jsonText"/> là JSON của chính luật này.</summary>
        public void Bind(RecurringRuleModel model, RecurringPrefixDraft draft, string jsonText, string occurrencesSubtitle, bool atOccurrenceLimit)
        {
            _model = model ?? RecurringRuleModel.Empty(string.Empty);
            _draft = draft ?? RecurringPrefixDraft.None;
            bool hasRule = _model.HasRule;
            EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !hasRule);
            if (!hasRule) return;

            RecurringLiveEventRule rule = _model.Rule;
            _sentence.SetTokens(_model.SentenceTokens);
            _typeField.choices = new List<string> { rule.EventType };
            _typeField.SetValueWithoutNotify(rule.EventType);
            _presetField.SetValueWithoutNotify(PresetNameOf(_model.PresetIndex));
            _prefixField.SetValueWithoutNotify(rule.EffectiveIdPrefix);
            _periodField.SetValueWithoutNotify(rule.PeriodHours);
            _activeField.SetValueWithoutNotify(rule.ActiveHours);
            BindAnchor(rule);

            SetSuffix(RecurringRuleFields.PeriodHours, EqualsText(rule.PeriodHours));
            SetSuffix(RecurringRuleFields.ActiveHours, EqualsText(rule.ActiveHours));
            SetSuffix(RecurringRuleFields.IdPrefix, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.RecurringIdPrefixDefaultFormat, rule.EventType + DefaultPrefixSuffix));

            SetError(RecurringRuleFields.Anchor);
            SetError(RecurringRuleFields.PeriodHours);
            SetError(RecurringRuleFields.ActiveHours);
            SetError(RecurringRuleFields.IdPrefix);

            _cycleBar.PeriodHours = rule.PeriodHours;
            _cycleBar.ActiveHours = rule.ActiveHours;
            _cycleBar.SetColorSlot(_model.ColorSlot);
            _cycleText.text = _model.CycleText;
            // (UX-22) Luật hỏng thì câu nhịp đọc như lỗi: thanh chu kỳ đã vẽ phần tràn bằng màu chặn, chữ dưới nó phải
            // nói cùng một chuyện chứ không đọc như một ghi chú bình thường.
            _cycleText.EnableInClassList(LiveOpsHubClassNames.TextBlocked, !_model.IsValid);

            _anchorNotice.text = _model.AnchorNotice;
            _anchorNotice.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, _model.AnchorNotice.Length == 0);

            BindDraftBlock();
            BindAfterWriteBlock();

            _jsonFoldout.Bind(_model.EventType, jsonText);
            _occurrences.SetRows(_model.NextOccurrences, occurrencesSubtitle, atOccurrenceLimit, _model.OccurrencesEmptyReason);
        }

        /// <summary>Chuyển focus tới một field (bấm token trong câu đọc).</summary>
        public void FocusField(string fieldName)
        {
            FieldSlot slot;
            if (fieldName == null || !_slots.TryGetValue(fieldName, out slot)) return;
            slot.Input.Focus();
            _sentence.SetFocusedField(fieldName);
        }

        internal const string DefaultPrefixSuffix = "-";

        private void BindAnchor(RecurringLiveEventRule rule)
        {
            DateTime anchorUtc;
            if (rule.TryGetAnchorUtc(out anchorUtc))
            {
                _anchorField.SetValueWithoutNotify(anchorUtc);
                SetSuffix(RecurringRuleFields.Anchor, string.Empty);
                SetSubLine(RecurringRuleFields.Anchor, _model.AnchorDeviceLine);
                return;
            }
            // Giờ không đọc được giữ NGUYÊN VĂN trong ô: sửa hộ người dùng là làm mất bằng chứng của chỗ hỏng.
            _anchorField.SetRawTextWithoutNotify(rule.AnchorUtcText, string.Empty);
            SetSuffix(RecurringRuleFields.Anchor, string.Empty);
            SetSubLine(RecurringRuleFields.Anchor, string.Empty);
        }

        private void BindDraftBlock()
        {
            bool hasDraft = _draft.NeedsConfirmation && string.Equals(_draft.EventType, _model.EventType, StringComparison.Ordinal);
            if (!hasDraft)
            {
                _draftBlock.RemoveFromHierarchy();
                MarkDrafting(string.Empty);
                return;
            }
            // Nháp của một ô lạ (thao tác mới quên khai hằng) vẫn phải hiện ở đâu đó: treo vào ô Tiền tố id và khoá theo
            // CHÍNH ô đó, không theo tên lạ — nếu không, ba ô kia khoá mà không ô nào mở để đi tiếp.
            string draftFieldName = _slots.ContainsKey(_draft.FieldName) ? _draft.FieldName : RecurringRuleFields.IdPrefix;
            FieldSlot slot = SlotOf(draftFieldName);
            slot.NoticeHost.Add(_draftBlock);
            // (UX-22) Giá trị hỏng (game sẽ BỎ HẲN luật) không ghi được, nên câu hậu quả "đợt weekly-pass-35 sẽ biến mất"
            // và nút danger "Ghi giá trị mới…" là lời mời làm một việc không làm được. Còn lại đúng hai thứ: dòng lỗi
            // dưới ô (SetError đã in) và lối ra "Huỷ (Esc)".
            bool isWritable = _model.FieldErrorText(draftFieldName).Length == 0;
            _draftNotice.text = isWritable ? _draft.CellNotice : string.Empty;
            _draftHelpBox.text = isWritable ? _draft.ConsequenceText : string.Empty;
            _draftWriteButton.text = _draft.WriteButtonText;
            _draftNotice.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !isWritable);
            _draftHelpBox.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !isWritable);
            _draftWriteButton.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !isWritable);
            MarkDrafting(draftFieldName);
        }

        private void BindAfterWriteBlock()
        {
            bool hasNotice = _model.AfterWriteNotice.Length > 0;
            _afterWriteBlock.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !hasNotice);
            if (!hasNotice) return;
            _afterWriteHelpBox.text = _model.AfterWriteNotice;
            _revertPrefix = _model.AfterWriteRevertPrefix;
            _afterWriteRevertButton.text = string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.RecurringRevertPrefixButtonFormat, _revertPrefix);
        }

        /// <summary>
        /// (Q-W4-4, user duyệt 16/9) Ô đang giữ nháp được viền warning; BA ô còn lại bị KHOÁ tới khi nháp được ghi hoặc huỷ,
        /// kèm lý do in THÀNH CHỮ ngay cạnh ô (SPIKE-B SP-3 — chữ là đường chính, tooltip là đường phụ; cả hai đều có
        /// test gác: <c>RecurringRulesSectionTests.AssertFieldLocked/AssertFieldUnlocked</c> so cả câu lẫn tooltip).
        /// <para>
        /// Vì sao khoá: nháp sống ở ĐÚNG MỘT ô (<see cref="RecurringPrefixDraft"/>), nên gõ tiếp vào ô thứ hai sẽ thay nháp
        /// cũ bằng nháp mới và cái vừa gõ ở ô thứ nhất biến mất không dấu vết — người dùng tưởng cả hai đang chờ ghi.
        /// </para>
        /// </summary>
        private void MarkDrafting(string fieldName)
        {
            bool hasDraft = fieldName.Length > 0;
            string draftingLabelText = hasDraft ? SlotOf(fieldName).Label.text : string.Empty;
            // (UX-23) MỘT câu cho cả nhóm, đặt ở ô đầu tiên bị khoá: in cùng một câu dưới từng ô thì màn đọc như ba lỗi
            // khác nhau đang xảy ra cùng lúc, trong khi chỉ có một việc phải làm và nó ở ô thứ tư.
            string lockSentence = hasDraft
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringFieldsLockedByDraftFormat, draftingLabelText)
                : string.Empty;
            bool sentenceShown = false;
            for (int index = 0; index < _slotOrder.Count; index++)
            {
                string slotFieldName = _slotOrder[index];
                FieldSlot slot = SlotOf(slotFieldName);
                bool isDrafting = hasDraft && string.Equals(slotFieldName, fieldName, StringComparison.Ordinal);
                slot.Input.EnableInClassList(LiveOpsHubClassNames.RecurringFieldDrafting, isDrafting);
                bool isLocked = hasDraft && !isDrafting;
                slot.Input.SetEnabled(!isLocked);
                bool showsSentence = isLocked && !sentenceShown;
                if (showsSentence) sentenceShown = true;
                slot.LockReason.text = showsSentence ? lockSentence : string.Empty;
                // Tooltip vẫn đặt trên MỌI ô bị khoá: câu in ra chỉ có một chỗ, nhưng người rê chuột lên ô thứ ba cũng
                // phải đọc được vì sao nó không gõ được (SP-3 nói tooltip là đường phụ, không phải đường duy nhất).
                slot.Input.tooltip = isLocked ? lockSentence : string.Empty;
                slot.LockReason.tooltip = slot.LockReason.text;
                slot.LockReason.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !showsSentence);
            }
            // Foldout JSON sửa được cùng một luật bằng một đường khác, nên nó cũng phải khoá: "Áp" trong lúc còn nháp sẽ
            // dựng nháp mới đè nháp cũ. Ô nhập vẫn mở (đọc/sao chép được), chỉ nút ghi khoá kèm ĐÚNG câu lý do của Q-W4-4.
            _jsonFoldout.SetLockedByDraft(hasDraft
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringFieldLockedByDraftFormat, draftingLabelText)
                : string.Empty);
        }

        /// <summary>
        /// (UX-23, UJ-21) Bấm vào một ô đang bị khoá hiện không phản hồi gì — người dùng bấm lại vài lần rồi bỏ. Ô khoá
        /// không nhận được sự kiện của chính nó (UI Toolkit bỏ qua callback trên cây đã tắt), nên form bắt ở gốc rồi tự
        /// dò xem con trỏ rơi vào hàng nào: rơi vào hàng bị khoá thì đưa focus TỚI NÚT "Huỷ (Esc)" và nháy nút đó.
        /// <para>
        /// Focus đi tới nút Huỷ chứ không về ô đang giữ nháp: ô nháp đã có viền warning và con trỏ vừa rời khỏi nó, còn
        /// thứ người dùng đang thiếu là LỐI RA. Cổng hành trình của gói G khoá đúng hành vi này
        /// (<c>UxRecurringJourneyTests.PrefixDraft_LocksFormWithOneSentence_AndClickOnLockedFieldFocusesCancel</c>).
        /// </para>
        /// <para>
        /// Gác bằng CÙNG điều kiện với <see cref="BindDraftBlock"/> (nháp phải thuộc đúng luật đang mở): nháp của luật
        /// khác thì khối nháp đã bị gỡ khỏi cây, bấm ô sẽ cướp focus rồi nháy một nút không còn trên màn.
        /// </para>
        /// </summary>
        private void OnPointerDownInsideForm(PointerDownEvent pointerEvent)
        {
            if (!_draft.NeedsConfirmation) return;
            if (!string.Equals(_draft.EventType, _model.EventType, StringComparison.Ordinal)) return;
            string draftFieldName = _slots.ContainsKey(_draft.FieldName) ? _draft.FieldName : RecurringRuleFields.IdPrefix;
            Vector2 position = new Vector2(pointerEvent.position.x, pointerEvent.position.y);
            for (int index = 0; index < _slotOrder.Count; index++)
            {
                string slotFieldName = _slotOrder[index];
                if (string.Equals(slotFieldName, draftFieldName, StringComparison.Ordinal)) continue;
                if (!SlotOf(slotFieldName).Row.worldBound.Contains(position)) continue;
                // Focus đặt ở LƯỢT SAU: panel tự xử lý focus của chính cú bấm này sau khi trickle-down chạy xong, nên gọi
                // Focus() ngay ở đây thì nó bị cú bấm ghi đè và con trỏ không đi đâu cả.
                _draftCancelButton.schedule.Execute(() => _draftCancelButton.Focus());
                FlashDraftCancel();
                return;
            }
        }

        /// <summary>Đủ để transition 300ms của <c>liveops-hub-row--flash</c> chạy hết rồi mới gỡ class (motion USS).</summary>
        private const long FlashMilliseconds = 300;

        /// <summary>Bộ hẹn giờ gỡ class chớp của lần nháy ĐANG chạy — lần nháy mới phải huỷ nó, xem <see cref="FlashDraftCancel"/>.</summary>
        private IVisualElementScheduledItem _flashReleaseSchedule;

        /// <summary>
        /// (UJ-21) Bấm ô khoá nhiều lần liên tiếp phải nháy được nhiều lần. Gỡ rồi thêm lại class trong CÙNG một khung thì
        /// computed style không đổi, transition không chạy lại và từ lần thứ hai người dùng không thấy gì: gỡ ở khung này,
        /// thêm lại ở khung sau. Bộ hẹn giờ của lần trước cũng phải huỷ, nếu không nó gỡ class giữa lần nháy đang chạy.
        /// </summary>
        private void FlashDraftCancel()
        {
            if (_flashReleaseSchedule != null)
            {
                _flashReleaseSchedule.Pause();
                _flashReleaseSchedule = null;
            }
            _draftCancelButton.RemoveFromClassList(LiveOpsHubClassNames.RowFlash);
            _draftCancelButton.schedule.Execute(() =>
            {
                _draftCancelButton.AddToClassList(LiveOpsHubClassNames.RowFlash);
                _flashReleaseSchedule = _draftCancelButton.schedule
                    .Execute(() => _draftCancelButton.RemoveFromClassList(LiveOpsHubClassNames.RowFlash))
                    .StartingIn(FlashMilliseconds);
            });
        }

        private string PresetNameOf(int presetIndex)
        {
            return presetIndex >= 0 && presetIndex < _presetNames.Count ? _presetNames[presetIndex] : LiveOpsHubStrings.RecurringPresetCustom;
        }

        private static string EqualsText(int hours)
        {
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringEqualsFormat,
                RecurringRuleModel.HoursText(hours, FormatForSuffix));
        }

        /// <summary>Chữ "= 7 ngày" chỉ đổi giờ sang ngày nên không cần lệch múi giờ của máy — dùng format lệch 0.</summary>
        private static readonly LiveOpsHubFormat FormatForSuffix = new LiveOpsHubFormat(TimeSpan.Zero);

        private void SetSuffix(string fieldName, string text)
        {
            FieldSlot slot = SlotOf(fieldName);
            slot.Suffix.text = text ?? string.Empty;
            slot.Suffix.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, slot.Suffix.text.Length == 0);
        }

        private void SetSubLine(string fieldName, string text)
        {
            FieldSlot slot = SlotOf(fieldName);
            slot.SubLine.text = text ?? string.Empty;
            slot.SubLine.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, slot.SubLine.text.Length == 0);
        }

        private void SetError(string fieldName)
        {
            FieldSlot slot = SlotOf(fieldName);
            string error = _model.FieldErrorText(fieldName);
            slot.Error.text = error;
            slot.Error.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, error.Length == 0);
            // (UX-22) Viền chặn ngay tại ô: một dòng chữ đỏ ở dưới không nói được "chính ô NÀY đang giữ giá trị hỏng".
            slot.Input.EnableInClassList(LiveOpsHubClassNames.RecurringFieldInvalid, error.Length > 0);
        }

        private void AddFieldRow(string fieldName, string labelText, VisualElement input, bool registerSlot)
        {
            VisualElement group = new VisualElement();
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.RecurringFieldRow);
            Label label = new Label(labelText);
            label.AddToClassList(LiveOpsHubClassNames.RecurringFieldLabel);
            row.Add(label);
            input.AddToClassList(LiveOpsHubClassNames.RecurringFieldInput);
            row.Add(input);
            Label suffix = new Label();
            suffix.AddToClassList(LiveOpsHubClassNames.RecurringFieldSuffix);
            row.Add(suffix);
            group.Add(row);

            VisualElement noticeHost = new VisualElement();
            group.Add(noticeHost);
            // Lý do khoá đứng TRƯỚC dòng lỗi: khi ô vừa khoá vừa lỗi, câu "vì sao không gõ được" phải đọc trước câu lỗi cũ.
            // (UX-32) Dòng phụ đứng RIÊNG dưới ô (giờ máy của Neo): nối đuôi hàng ô thì hàng dài gấp đôi các hàng khác
            // và ở 820 nó là thứ bị cắt đầu tiên.
            Label subLine = new Label();
            subLine.AddToClassList(LiveOpsHubClassNames.RecurringFieldSubLine);
            subLine.AddToClassList(LiveOpsHubClassNames.RecurringHidden);
            noticeHost.Add(subLine);
            Label lockReason = new Label();
            lockReason.AddToClassList(LiveOpsHubClassNames.RecurringFieldLockReason);
            // Chữ PHỤ, không phải chữ lỗi: ô bị khoá là trạng thái tạm và lành (chờ ghi/huỷ nháp), tô đỏ cả ba dòng thì màn
            // đọc như ba lỗi cùng lúc. TextBlocked ở dưới vẫn dành cho dòng lỗi thật của chính field.
            lockReason.AddToClassList(LiveOpsHubClassNames.TextQuiet);
            lockReason.AddToClassList(LiveOpsHubClassNames.RecurringHidden);
            noticeHost.Add(lockReason);
            Label error = new Label();
            error.AddToClassList(LiveOpsHubClassNames.TextBlocked);
            // (UX-22) Dòng lỗi thụt theo cột Ô, không nằm dưới cột nhãn: mắt đọc lỗi ngay dưới chỗ vừa gõ.
            error.AddToClassList(LiveOpsHubClassNames.RecurringFieldError);
            noticeHost.Add(error);
            Add(group);

            if (!registerSlot) return;
            _slots[fieldName] = new FieldSlot(group, row, noticeHost, label, input, suffix, error, lockReason, subLine);
            _slotOrder.Add(fieldName);
        }

        private FieldSlot SlotOf(string fieldName)
        {
            return _slots[fieldName];
        }

        private void RegisterFieldCallbacks()
        {
            _prefixField.RegisterValueChangedCallback(changeEvent => RequestChange(RecurringRuleFields.IdPrefix,
                rule => rule.WithIdPrefix(changeEvent.newValue)));
            _periodField.RegisterValueChangedCallback(changeEvent => RequestChange(RecurringRuleFields.PeriodHours,
                rule => rule.WithPeriodHours(changeEvent.newValue)));
            _activeField.RegisterValueChangedCallback(changeEvent => RequestChange(RecurringRuleFields.ActiveHours,
                rule => rule.WithActiveHours(changeEvent.newValue)));
            _anchorField.RegisterValueChangedCallback(changeEvent => RequestChange(RecurringRuleFields.Anchor,
                rule => rule.WithAnchor(LiveEventUtcText.Format(changeEvent.newValue))));
            _presetField.RegisterValueChangedCallback(OnPresetChanged);

            foreach (KeyValuePair<string, FieldSlot> pair in _slots)
            {
                string fieldName = pair.Key;
                FieldSlot slot = pair.Value;
                slot.Input.RegisterCallback<FocusInEvent>(focusEvent => OnFieldFocusIn(fieldName));
                slot.Input.RegisterCallback<FocusOutEvent>(focusEvent => OnFieldFocusOut(fieldName));
                // Esc tại ô = "Huỷ (Esc)" (mục 7.4): nháp chỉ mất ở đây, không mất khi đóng hộp xác nhận.
                slot.Input.RegisterCallback<KeyDownEvent>(OnFieldKeyDown, TrickleDown.TrickleDown);
            }
        }

        private void OnFieldKeyDown(KeyDownEvent keyDownEvent)
        {
            if (keyDownEvent.keyCode != KeyCode.Escape || !_draft.NeedsConfirmation) return;
            RaiseDraftCancel();
            keyDownEvent.StopPropagation();
        }

        private void OnFieldFocusIn(string fieldName)
        {
            SlotOf(fieldName).Label.AddToClassList(LiveOpsHubClassNames.RecurringFieldLabelFocused);
            _sentence.SetFocusedField(fieldName);
        }

        private void OnFieldFocusOut(string fieldName)
        {
            SlotOf(fieldName).Label.RemoveFromClassList(LiveOpsHubClassNames.RecurringFieldLabelFocused);
            _sentence.SetFocusedField(string.Empty);
        }

        /// <summary>
        /// Chọn mẫu theo CHỈ SỐ của dropdown, không dò theo tên hiển thị: hai thư viện mẫu của dự án được phép đặt trùng
        /// <c>DisplayName</c>, và dò theo chuỗi thì mẫu thứ hai không bao giờ áp được. Chỉ số cuối là "Tùy chỉnh…" — không áp gì.
        /// </summary>
        private void OnPresetChanged(ChangeEvent<string> changeEvent)
        {
            if (PresetSelected == null) return;
            int presetIndex = _presetField.index;
            if (presetIndex < 0 || presetIndex >= _presetNames.Count) return;
            PresetSelected(presetIndex);
        }

        private void RequestChange(string fieldName, Func<RecurringLiveEventRule, RecurringLiveEventRule> apply)
        {
            if (!_model.HasRule || ValueChangeRequested == null) return;
            RecurringLiveEventRule candidate = apply(_model.WrittenRule);
            ValueChangeRequested(fieldName, candidate);
        }

        private void RaiseDraftCancel()
        {
            if (DraftCancelRequested != null) DraftCancelRequested();
        }

        private void RaiseDraftWrite()
        {
            if (DraftWriteRequested != null) DraftWriteRequested();
        }

        private void RaiseRevertPrefix()
        {
            if (RevertPrefixRequested != null) RevertPrefixRequested();
        }

        private void RaiseDecideInValidation()
        {
            if (DecideInValidationRequested != null) DecideInValidationRequested();
        }

        private void RaiseAddMoreOccurrences()
        {
            if (AddMoreOccurrencesRequested != null) AddMoreOccurrencesRequested();
        }

        private void RaiseCopyJson()
        {
            if (CopyJsonRequested != null) CopyJsonRequested();
        }

        private void RaiseApplyJson(RecurringLiveEventRule candidate)
        {
            if (ApplyJsonRequested != null) ApplyJsonRequested(candidate);
        }

        private void RaiseDelete()
        {
            if (DeleteRequested != null) DeleteRequested();
        }

        /// <summary>Một ô của form: nhóm, hàng, chỗ treo chú thích/nháp, nhãn, control, chữ phụ và dòng lỗi.</summary>
        private sealed class FieldSlot
        {
            internal FieldSlot(VisualElement group, VisualElement row, VisualElement noticeHost, Label label, VisualElement input,
                Label suffix, Label error, Label lockReason, Label subLine)
            {
                SubLine = subLine;
                Group = group;
                Row = row;
                NoticeHost = noticeHost;
                Label = label;
                Input = input;
                Suffix = suffix;
                Error = error;
                LockReason = lockReason;
            }

            public VisualElement Group { get; }
            public VisualElement Row { get; }
            public VisualElement NoticeHost { get; }
            public Label Label { get; }
            public VisualElement Input { get; }
            public Label Suffix { get; }
            public Label Error { get; }

            /// <summary>Dòng phụ dưới ô (giờ máy của Neo) — không phải chữ phụ cuối hàng ô.</summary>
            public Label SubLine { get; }

            /// <summary>Lý do ô đang bị khoá, in thành chữ (Q-W4-4 + SPIKE-B SP-3); rỗng và ẩn khi ô mở.</summary>
            public Label LockReason { get; }
        }
    }
}
