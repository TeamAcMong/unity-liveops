using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Popover "Bỏ qua cảnh báo…" ([SD2 §2.7], Hình 17 ô 5). Ba luật của thiết kế:
    /// <list type="bullet">
    /// <item><b>Ghi chú bắt buộc</b> — một cảnh báo bị ẩn mà không nói được vì sao là cái bẫy cho người đọc lịch tháng sau;
    /// nút "Bỏ qua" khoá tới khi có chữ, và lý do khoá in THÀNH CHỮ cạnh nút (SPIKE-B SP-3), không chỉ tooltip.</item>
    /// <item><b>Phạm vi gắn với KHOẢNG</b> (luật + đích + khoảng thời gian): khoảng đổi thì cảnh báo hiện lại. Tắt Toggle =
    /// mọi khoảng — hai mốc rỗng, đúng cách <c>LiveEventCalendarCheckRun.Matches</c> đọc "không giới hạn phía đó".</item>
    /// <item><b>Lưu trong asset lịch</b>, không phải EditorPrefs của một máy: "để review bằng git".</item>
    /// </list>
    /// Popover không tự đụng phiên (test dựng được nó mà không cần asset): nó trả <see cref="IgnoredCalendarWarning"/> đã dựng
    /// xong cho màn, màn mới là chỗ áp một Undo group + một toast + tự kiểm lại (PD-10).
    /// </summary>
    internal sealed class IgnoreWarningPopover : LiveOpsPopoverContent
    {
        /// <summary>300px theo mockup; cao đủ cho câu phạm vi hai dòng + ô ghi chú + footer.</summary>
        private static readonly Vector2 WindowSize = new Vector2(300f, 208f);

        private readonly LiveEventCalendarFinding _finding;
        private readonly LiveOpsHubFormat _format;
        private readonly ILiveOpsHubLayoutLoader _layoutLoader;
        private readonly string _calendarAssetName;
        private readonly Action<IgnoredCalendarWarning> _apply;

        private Toggle _scopeToggle;
        private Label _scopeLabel;
        private TextField _noteField;
        private LiveOpsButtonSlot _confirmSlot;

        internal IgnoreWarningPopover(LiveEventCalendarFinding finding, LiveOpsHubFormat format, ILiveOpsHubLayoutLoader layoutLoader,
            string calendarAssetName, Action<IgnoredCalendarWarning> apply)
        {
            _finding = finding ?? throw new ArgumentNullException(nameof(finding));
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _layoutLoader = layoutLoader ?? throw new ArgumentNullException(nameof(layoutLoader));
            _calendarAssetName = calendarAssetName ?? string.Empty;
            _apply = apply;
        }

        /// <summary>Số lần "Bỏ qua" đã chạy — test chứng minh ghi chú rỗng thì không bỏ qua được.</summary>
        internal int IgnoreCount { get; private set; }

        internal Button ConfirmButton => _confirmSlot == null ? null : _confirmSlot.Button;
        internal LiveOpsButtonSlot ConfirmSlot => _confirmSlot;
        internal Button CancelButton { get; private set; }
        internal Toggle ScopeToggle => _scopeToggle;

        /// <summary>Câu phạm vi — Label riêng cạnh ô tick (soát W5 F-2), không phải <c>Toggle.label</c>.</summary>
        internal Label ScopeLabel => _scopeLabel;
        internal TextField NoteField => _noteField;

        protected override Vector2 PopoverSize => WindowSize;

        /// <summary>Ô ghi chú nhận focus đầu tiên ([SD2 §2.7]): việc đầu tiên phải làm là viết vì sao, không phải bấm nút.</summary>
        protected override Focusable InitialFocus => _noteField;

        protected override VisualElement BuildContent()
        {
            VisualTreeAsset layout = _layoutLoader.LoadVisualTree(LiveOpsHubPaths.IgnoreWarningPopoverUxml);
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ValidationMissingLayoutFormat, LiveOpsHubPaths.IgnoreWarningPopoverUxml));
            }

            // Bỏ vỏ TemplateContainer (cùng lý do với ProposalPopover): vỏ không mang class nên nó không nhận bề rộng 300px.
            VisualElement root = layout.Instantiate().Q(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreBody) ?? layout.Instantiate();
            root.name = LiveOpsHubPaths.ValidationDepthElementNames.IgnoreBody;
            StyleSheet sheet = _layoutLoader.LoadStyleSheet(LiveOpsHubPaths.ValidationSectionUss);
            if (sheet != null) root.styleSheets.Add(sheet);

            Label header = root.Q<Label>(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreHeader);
            if (header != null) header.text = LiveOpsHubStrings.ValidationDepthIgnoreHeader;

            _scopeToggle = root.Q<Toggle>(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreScope);
            if (_scopeToggle != null)
            {
                // Bật sẵn, và khoá hẳn khi phát hiện không có khoảng: bật một Toggle không đổi được gì là nói dối về phạm vi.
                _scopeToggle.SetValueWithoutNotify(HasRange);
                _scopeToggle.SetEnabled(HasRange);
            }

            _scopeLabel = root.Q<Label>(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreScopeLabel);
            if (_scopeLabel != null) _scopeLabel.text = ScopeLabelText();

            Label noteLabel = root.Q<Label>(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreNoteLabel);
            if (noteLabel != null) noteLabel.text = LiveOpsHubStrings.ValidationDepthIgnoreNoteLabel;

            _noteField = root.Q<TextField>(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreNote);
            if (_noteField != null) _noteField.RegisterValueChangedCallback(changeEvent => RefreshConfirm());

            Label hint = root.Q<Label>(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreFooterHint);
            if (hint != null)
            {
                hint.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthIgnoreFooterFormat,
                    LiveOpsFindingText.NoParse(_calendarAssetName));
                hint.enableRichText = true;
            }

            CancelButton = root.Q<Button>(LiveOpsHubPaths.ValidationDepthElementNames.IgnoreCancel);
            if (CancelButton != null)
            {
                CancelButton.text = LiveOpsHubStrings.ValidationDepthIgnoreCancelButton;
                CancelButton.clicked += ClosePopover;
            }

            VisualElement footer = CancelButton != null ? CancelButton.parent : root;
            Button confirm = new Button(OnConfirmClicked)
            {
                name = LiveOpsHubPaths.ValidationDepthElementNames.IgnoreConfirm,
                text = LiveOpsHubStrings.ValidationDepthIgnoreConfirmButton,
            };
            confirm.AddToClassList(LiveOpsHubClassNames.Button);
            confirm.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _confirmSlot = new LiveOpsButtonSlot(confirm);
            footer.Add(_confirmSlot);

            RefreshConfirm();
            return root;
        }

        /// <summary>Phát hiện có khoảng thì bỏ qua gắn được với khoảng đó; không có thì bỏ qua luôn là "mọi khoảng".</summary>
        private bool HasRange => _finding.RangeStartUtc.HasValue || _finding.RangeEndUtc.HasValue;

        private string ScopeLabelText()
        {
            if (!HasRange) return LiveOpsHubStrings.ValidationDepthIgnoreScopeEveryRange;
            return LiveOpsFindingText.PlainText(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.ValidationDepthIgnoreScopeFormat, RangeText(), LiveOpsFindingText.NoParse(_finding.TargetId)));
        }

        /// <summary>Khoảng viết theo cùng khuôn mà nhóm "Đã bỏ qua" dùng — hai chỗ nói về một khoảng phải đọc y như nhau.</summary>
        private string RangeText()
        {
            return LiveOpsFindingText.IgnoredWarningRangeText(BuildWarning(true, string.Empty), _format);
        }

        private void RefreshConfirm()
        {
            if (_confirmSlot == null) return;
            bool hasNote = NoteText().Length > 0;
            _confirmSlot.SetEnabledWithReason(hasNote, hasNote ? string.Empty : LiveOpsHubStrings.ValidationDepthIgnoreNoteRequiredReason);
        }

        private string NoteText()
        {
            return _noteField == null || _noteField.value == null ? string.Empty : _noteField.value.Trim();
        }

        /// <summary>
        /// Cảnh báo sẽ ghi vào asset. Toggle tắt = hai mốc RỖNG: <c>LiveEventCalendarCheckRun</c> đọc mốc rỗng là "không giới
        /// hạn phía đó", nên rỗng cả hai đúng nghĩa "mọi khoảng" của thiết kế.
        /// </summary>
        private IgnoredCalendarWarning BuildWarning(bool limitToRange, string note)
        {
            string startText = limitToRange && _finding.RangeStartUtc.HasValue
                ? LiveEventUtcText.Format(_finding.RangeStartUtc.Value)
                : string.Empty;
            string endText = limitToRange && _finding.RangeEndUtc.HasValue
                ? LiveEventUtcText.Format(_finding.RangeEndUtc.Value)
                : string.Empty;
            // Bỏ qua bằng tay KHÔNG có hạn: ghi chú hẹn giờ chỉ sinh ra từ mục 2 của menu "Quyết định…" ([SD2 §2.7]).
            return new IgnoredCalendarWarning(_finding.RuleId, _finding.TargetId, startText, endText, note, string.Empty);
        }

        private void OnConfirmClicked()
        {
            string note = NoteText();
            if (note.Length == 0) return;
            IgnoreCount++;
            _apply?.Invoke(BuildWarning(_scopeToggle != null && _scopeToggle.value, note));
            ClosePopover();
        }
    }
}
