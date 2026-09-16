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
        private readonly Func<LiveEventCalendarRepair, int> _quickCheck;
        private readonly Action<LiveEventCalendarRepair> _apply;
        private readonly List<Label> _optionDetails = new List<Label>();

        private RadioButtonGroup _options;
        private LiveOpsStateMark _quickCheckMark;
        private Label _quickCheckLabel;
        private Label _consequenceLabel;
        private int _selectedIndex;

        /// <summary>
        /// <paramref name="quickCheck"/> trả SỐ vấn đề còn lại trên làn sau khi áp một cách sửa (màn chạy <c>CheckLane</c> trên
        /// nháp xem trước; âm = không kiểm được) — số chứ không phải câu, vì popover còn phải đổi màu dấu theo kết quả và đọc
        /// ngược câu để đoán màu là chỗ dễ nói dối nhất. <paramref name="apply"/> áp cách sửa đã chọn. Popover không tự đụng
        /// phiên để test dựng được nó mà không cần asset.
        /// </summary>
        internal ProposalPopover(LiveEventCalendarFinding finding, LiveOpsHubFormat format, ILiveOpsHubLayoutLoader layoutLoader,
            Func<LiveEventCalendarRepair, int> quickCheck, Action<LiveEventCalendarRepair> apply, int preselectedIndex)
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

            // Bỏ vỏ TemplateContainer (cùng lý do với ValidationSection): vỏ không mang class nào nên nó không nhận bề rộng
            // 320px của popover, và ảnh chụp ô 7 Hình 17 sẽ ra một dải chữ rộng bằng cả cửa sổ thay vì một popover.
            VisualElement root = layout.Instantiate().Q(BodyElementName) ?? layout.Instantiate();
            root.name = BodyElementName;
            // Cửa sổ popover là panel riêng chỉ nạp theme + components + feedback + motion (LiveOpsFeedbackStyleSheets.PopoverSheets),
            // nên ValidationSection.uss KHÔNG tới đây — mà UXML của popover dùng đúng các class trong sheet đó (bề rộng 320,
            // header đậm, dòng chi tiết 10px, footer một hàng). Thiếu sheet thì popover rơi về mặc định Unity: rộng bằng panel và
            // hai nút xếp chồng. Gắn sheet ngay trên cây của popover là chỗ duy nhất biết mình cần sheet nào.
            StyleSheet sheet = _layoutLoader.LoadStyleSheet(LiveOpsHubPaths.ValidationSectionUss);
            if (sheet != null) root.styleSheets.Add(sheet);

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
                _quickCheckMark = new LiveOpsStateMark();
                _quickCheckMark.Size = LiveOpsStateMark.MarkSize.Small;
                quickCheckHost.Add(_quickCheckMark);
                _quickCheckLabel = new Label();
                quickCheckHost.Add(_quickCheckLabel);
            }

            _consequenceLabel = root.Q<Label>(ConsequenceElementName);
            if (_consequenceLabel != null) _consequenceLabel.enableRichText = true;

            Label hint = root.Q<Label>(HintElementName);
            if (hint != null) hint.text = LiveOpsHubStrings.ValidationProposalEnterHint;

            ApplyButton = root.Q<Button>(ApplyElementName);
            // Chữ nút áp do RefreshSelection đổ: nó phải nói động từ của lựa chọn ĐANG chọn, nên không gán một lần ở đây.
            if (ApplyButton != null) ApplyButton.clicked += OnApplyClicked;

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

        /// <summary>
        /// Dòng 10px của một lựa chọn ([SD2 §2.6]: "bắt đầu 16/9 12:00 → 17/9 00:00 · dài 24 giờ"). Core trả
        /// <c>BeforeText</c>/<c>AfterText</c> là chuỗi ISO thô có chủ đích (V-8: câu là việc của Editor) — in thẳng ra thì người
        /// đọc phải tự dịch "2026-09-16T12:00:00Z" trong đầu. Cách sửa không có khung giờ mới (đổi tên, đặt lại chu kỳ) thì giá
        /// trị thô CHÍNH LÀ thứ phải đọc, nên vẫn giữ nguyên nó.
        /// </summary>
        private string DetailTextOf(LiveEventCalendarRepair repair)
        {
            if (repair.NewStartUtc.HasValue && repair.NewEndUtc.HasValue && _finding.RangeStartUtc.HasValue)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationProposalOptionDetailFormat,
                    _format.ShortDateTime(_finding.RangeStartUtc.Value), _format.ShortDateTime(repair.NewStartUtc.Value),
                    EventLengthOf(repair.NewEndUtc.Value - repair.NewStartUtc.Value));
            }

            string before = LiveOpsFindingText.NoParse(repair.BeforeText);
            string after = LiveOpsFindingText.NoParse(repair.AfterText);
            if (before.Length == 0 && after.Length == 0) return string.Empty;
            return before.Length > 0 && after.Length > 0 ? before + BeforeAfterArrow + after : before + after;
        }

        private const string BeforeAfterArrow = " → ";

        /// <summary>"24 giờ" khi độ dài chẵn giờ — cùng cách viết mà câu lựa chọn của <see cref="LiveOpsFindingText"/> dùng.</summary>
        private string EventLengthOf(TimeSpan length)
        {
            if (length > TimeSpan.Zero && length.Ticks % TimeSpan.TicksPerHour == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.FindingHoursFormat,
                    ((long)length.TotalHours).ToString(CultureInfo.InvariantCulture));
            }
            return _format.Duration(length, false);
        }

        private void OnOptionChanged(ChangeEvent<int> changeEvent)
        {
            _selectedIndex = changeEvent.newValue;
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _finding.Repairs.Count) return;
            LiveEventCalendarRepair repair = _finding.Repairs[_selectedIndex];
            RefreshQuickCheck(repair);
            if (_consequenceLabel != null) _consequenceLabel.text = ConsequenceTextOf(repair);
            if (ApplyButton != null) ApplyButton.text = ApplyTextOf(repair);
        }

        /// <summary>
        /// Dấu + câu kiểm nhanh của lựa chọn đang chọn. Dấu PHẢI đi theo kết quả: gán Ok một lần lúc dựng thì câu "vẫn còn 2 vấn
        /// đề" đứng cạnh một dấu xanh — người đọc tin dấu trước khi đọc câu ([SD2 §2.6] gắn dấu Ok với ca "không chồng").
        /// </summary>
        private void RefreshQuickCheck(LiveEventCalendarRepair repair)
        {
            int remaining = _quickCheck != null ? _quickCheck(repair) : 0;
            if (_quickCheckMark != null)
            {
                _quickCheckMark.SetHealth(remaining < 0 ? HealthState.NotMeasured : (remaining == 0 ? HealthState.Ok : HealthState.Warning));
            }
            if (_quickCheckLabel == null) return;
            if (remaining < 0) _quickCheckLabel.text = string.Empty;
            else if (remaining == 0) _quickCheckLabel.text = LiveOpsHubStrings.ValidationProposalQuickCheckOk;
            else
            {
                _quickCheckLabel.text = string.Format(CultureInfo.InvariantCulture,
                    LiveOpsHubStrings.ValidationProposalQuickCheckOverlapFormat, remaining);
            }
        }

        /// <summary>
        /// Câu hậu quả nói kết quả SAU KHI ÁP lựa chọn đang chọn ([SD2 §2.6]: "Đợt sẽ xuất hiện với người chơi từ 17/9 00:00
        /// UTC."). Câu của phát hiện nói chuyện ngược lại — chuyện xảy ra nếu KHÔNG sửa — nên chỉ dùng nó khi cách sửa không có
        /// giờ bắt đầu mới để nói.
        /// </summary>
        private string ConsequenceTextOf(LiveEventCalendarRepair repair)
        {
            if (!repair.NewStartUtc.HasValue) return LiveOpsFindingText.ConsequenceSentence(_finding, _format);
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationProposalConsequenceFormat,
                _format.ShortDateTimeUtc(repair.NewStartUtc.Value));
        }

        /// <summary>
        /// "Áp: dời hunt-0916-bonus" ([SD2 §2.6]) — nút nêu ĐỘNG TỪ của lựa chọn đang chọn, không chỉ id: bấm "Áp: hunt-0916-bonus"
        /// thì người bấm không biết mình sắp dời, sắp đổi tên hay sắp hoàn về. Mỗi <c>RepairId</c> một khuôn riêng (V-34, nợ Findings
        /// mang từ cổng W4 sang); khuôn trung tính chỉ còn cho lệnh sửa mà gói chưa biết — luật của game tự viết cũng ra được
        /// <c>RepairId</c> lạ, và một nút "Áp: …" vẫn phải bấm được.
        /// </summary>
        private string ApplyTextOf(LiveEventCalendarRepair repair)
        {
            return string.Format(CultureInfo.InvariantCulture, ApplyFormatOf(repair.RepairId), _finding.TargetId);
        }

        /// <summary>Khuôn chữ nút áp theo <c>RepairId</c> — tách khỏi <see cref="ApplyTextOf"/> để test duyệt được cả chín id.</summary>
        internal static string ApplyFormatOf(string repairId)
        {
            switch (repairId)
            {
                case LiveOpsFindingText.ShiftStartKeepEndRepairId:
                case LiveOpsFindingText.ShiftWholeKeepDurationRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplyShiftFormat;
                case LiveOpsFindingText.RenameRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplyRenameFormat;
                case LiveOpsFindingText.NormalizeRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplyNormalizeFormat;
                case LiveOpsFindingText.KeepStartSetDurationRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplySetDurationFormat;
                case LiveOpsFindingText.SwapStartEndRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplySwapFormat;
                case LiveOpsFindingText.SetActiveToPeriodRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplySetActiveFormat;
                case LiveOpsFindingText.RevertRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplyRevertFormat;
                case LiveOpsFindingText.DeferUntilEndRepairId:
                    return LiveOpsHubStrings.ValidationProposalApplyDeferFormat;
                default:
                    return LiveOpsHubStrings.ValidationProposalApplyFormat;
            }
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
