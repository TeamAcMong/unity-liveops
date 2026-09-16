using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Popover "Đề xuất…" ([SD2 §2.6]): mỗi lựa chọn ghi TRƯỚC → SAU, kèm một lần kiểm nhanh chỉ trên làn của đợt và câu hậu
    /// quả với người chơi. Hai luật an toàn của SPIKE-B nằm ở đây:
    /// <list type="bullet">
    /// <item><b>Enter = Quay lại</b> — phím mặc định làm việc AN TOÀN; "Áp" chỉ chạy khi có cú click thật.</item>
    /// <item>Nút áp (đổi điều người chơi thấy) KHÔNG phải nút mặc định và không nhận focus đầu tiên.</item>
    /// </list>
    /// Kiểm nhanh chỉ nói về làn đang sửa và KHÔNG làm màn Kiểm lịch chuyển sang Ok — kết quả của cả lịch vẫn phải kiểm lại.
    /// </summary>
    internal sealed class ProposalPopover : LiveOpsPopoverContent
    {
        internal const string BodyElementName = "proposal-body";
        internal const string HeaderElementName = "proposal-header";
        internal const string OptionsElementName = "proposal-options";
        internal const string QuickCheckElementName = "proposal-quick-check";
        internal const string ConsequenceElementName = "proposal-consequence";
        internal const string HintElementName = "proposal-hint";
        internal const string ApplyElementName = "proposal-apply";
        internal const string BackElementName = "proposal-back";

        /// <summary>320px là bề rộng chung của mọi popover hub ([FD §7]); chiều cao đủ cho hai lựa chọn hai dòng + footer.</summary>
        private static readonly Vector2 WindowSize = new Vector2(320f, 214f);

        private readonly LiveEventCalendarFinding _finding;
        private readonly LiveOpsHubFormat _format;
        private readonly ILiveOpsHubLayoutLoader _layoutLoader;
        private readonly Func<LiveEventCalendarRepair, string> _quickCheck;
        private readonly Action<LiveEventCalendarRepair> _apply;
        private readonly List<Label> _optionDetails = new List<Label>();

        private RadioButtonGroup _options;
        private Label _quickCheckLabel;
        private Label _consequenceLabel;
        private int _selectedIndex;

        /// <summary>
        /// <paramref name="quickCheck"/> trả câu kiểm nhanh của một cách sửa (màn chạy <c>CheckLane</c> trên nháp xem trước);
        /// <paramref name="apply"/> áp cách sửa đã chọn. Popover không tự đụng phiên để test dựng được nó mà không cần asset.
        /// </summary>
        internal ProposalPopover(LiveEventCalendarFinding finding, LiveOpsHubFormat format, ILiveOpsHubLayoutLoader layoutLoader,
            Func<LiveEventCalendarRepair, string> quickCheck, Action<LiveEventCalendarRepair> apply, int preselectedIndex)
        {
            _finding = finding ?? throw new ArgumentNullException(nameof(finding));
            _format = format ?? throw new ArgumentNullException(nameof(format));
            _layoutLoader = layoutLoader ?? throw new ArgumentNullException(nameof(layoutLoader));
            _quickCheck = quickCheck;
            _apply = apply;
            _selectedIndex = preselectedIndex >= 0 && preselectedIndex < finding.Repairs.Count ? preselectedIndex : 0;
        }

        /// <summary>Số lần "Áp" đã chạy — test chứng minh Enter KHÔNG áp.</summary>
        internal int ApplyCount { get; private set; }

        internal Button ApplyButton { get; private set; }
        internal Button BackButton { get; private set; }
        internal int SelectedIndex => _selectedIndex;

        protected override Vector2 PopoverSize => WindowSize;

        /// <summary>Focus đầu tiên là nút AN TOÀN (Quay lại) — nút áp không bao giờ nhận focus đầu (SPIKE-B SP-2).</summary>
        protected override Focusable InitialFocus => BackButton;

        protected override VisualElement BuildContent()
        {
            VisualTreeAsset layout = _layoutLoader.LoadVisualTree(LiveOpsHubPaths.ProposalPopoverUxml);
            if (layout == null)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ValidationMissingLayoutFormat, LiveOpsHubPaths.ProposalPopoverUxml));
            }

            VisualElement root = layout.Instantiate();
            root.name = BodyElementName;

            Label header = root.Q<Label>(HeaderElementName);
            if (header != null)
            {
                header.enableRichText = true;
                header.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationProposalHeaderFormat,
                    LiveOpsFindingText.NoParse(_finding.TargetId));
            }

            _options = root.Q<RadioButtonGroup>(OptionsElementName);
            BuildOptions();

            VisualElement quickCheckHost = root.Q(QuickCheckElementName);
            if (quickCheckHost != null)
            {
                LiveOpsStateMark mark = new LiveOpsStateMark();
                mark.SetHealth(HealthState.Ok);
                mark.Size = LiveOpsStateMark.MarkSize.Small;
                quickCheckHost.Add(mark);
                _quickCheckLabel = new Label();
                quickCheckHost.Add(_quickCheckLabel);
            }

            _consequenceLabel = root.Q<Label>(ConsequenceElementName);
            if (_consequenceLabel != null) _consequenceLabel.enableRichText = true;

            Label hint = root.Q<Label>(HintElementName);
            if (hint != null) hint.text = LiveOpsHubStrings.ValidationProposalEnterHint;

            ApplyButton = root.Q<Button>(ApplyElementName);
            if (ApplyButton != null)
            {
                ApplyButton.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationProposalApplyFormat, _finding.TargetId);
                ApplyButton.clicked += OnApplyClicked;
            }

            BackButton = root.Q<Button>(BackElementName);
            if (BackButton != null)
            {
                BackButton.text = LiveOpsHubStrings.ValidationProposalBackButton;
                BackButton.clicked += ClosePopover;
            }

            // Enter phải là việc an toàn kể cả khi focus đang ở radio hay ở nút áp: bắt ở gốc, TrickleDown, trước mọi nút.
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RefreshSelection();
            return root;
        }

        /// <summary>Test gửi phím mà không mở cửa sổ thật (cùng lối với <c>HandleKeyDownForTest</c> của lớp gốc).</summary>
        internal void HandleEnterForTest(KeyDownEvent keyDown)
        {
            OnKeyDown(keyDown);
        }

        internal void SelectOptionForTest(int index)
        {
            if (_options == null) return;
            _options.value = index;
        }

        private void BuildOptions()
        {
            if (_options == null) return;
            var choices = new List<string>(_finding.Repairs.Count);
            for (int index = 0; index < _finding.Repairs.Count; index++)
            {
                choices.Add(LiveOpsFindingText.RepairOptionText(_finding, _finding.Repairs[index], _format));
            }
            _options.choices = choices;
            _options.value = _selectedIndex;
            _options.RegisterValueChangedCallback(OnOptionChanged);

            // Dòng 10px "trước → sau" của từng lựa chọn: RadioButtonGroup chỉ dựng nhãn một dòng, nên gắn thêm vào chính RadioButton.
            _optionDetails.Clear();
            List<RadioButton> radioButtons = new List<RadioButton>(_options.Query<RadioButton>().ToList());
            for (int index = 0; index < radioButtons.Count && index < _finding.Repairs.Count; index++)
            {
                LiveEventCalendarRepair repair = _finding.Repairs[index];
                Label detail = new Label(DetailTextOf(repair)) { enableRichText = true };
                detail.AddToClassList(LiveOpsHubClassNames.ValidationProposalOptionDetail);
                radioButtons[index].AddToClassList(LiveOpsHubClassNames.ValidationProposalOption);
                radioButtons[index].Add(detail);
                _optionDetails.Add(detail);
            }
        }

        private string DetailTextOf(LiveEventCalendarRepair repair)
        {
            string before = LiveOpsFindingText.NoParse(repair.BeforeText);
            string after = LiveOpsFindingText.NoParse(repair.AfterText);
            if (before.Length == 0 && after.Length == 0) return string.Empty;
            return before.Length > 0 && after.Length > 0 ? before + BeforeAfterArrow + after : before + after;
        }

        private const string BeforeAfterArrow = " → ";

        private void OnOptionChanged(ChangeEvent<int> changeEvent)
        {
            _selectedIndex = changeEvent.newValue;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _finding.Repairs.Count) return;
            LiveEventCalendarRepair repair = _finding.Repairs[_selectedIndex];
            if (_quickCheckLabel != null)
            {
                _quickCheckLabel.text = _quickCheck != null ? _quickCheck(repair) : LiveOpsHubStrings.ValidationProposalQuickCheckOk;
            }
            if (_consequenceLabel != null) _consequenceLabel.text = LiveOpsFindingText.ConsequenceSentence(_finding, _format);
        }

        private void OnKeyDown(KeyDownEvent keyDown)
        {
            if (keyDown.keyCode != KeyCode.Return && keyDown.keyCode != KeyCode.KeypadEnter) return;
            keyDown.StopPropagation();
            EnterHandled = true;
            ClosePopover();
        }

        /// <summary>Test: Enter đã đi qua nhánh "Quay lại" (kể cả khi chưa có cửa sổ để đóng).</summary>
        internal bool EnterHandled { get; private set; }

        private void OnApplyClicked()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _finding.Repairs.Count) return;
            ApplyCount++;
            _apply?.Invoke(_finding.Repairs[_selectedIndex]);
            ClosePopover();
        }
    }
}
