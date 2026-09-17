using System;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Chế độ bắt lưới của toolbar [SD1 §3.1]; "Tự động" = bước theo zoom (15 phút / 1 giờ / 1 ngày).</summary>
    internal enum CalendarSnapMode
    {
        Automatic = 0,
        FifteenMinutes = 1,
        Hour = 2,
        Day = 3,
        Off = 4,
    }

    /// <summary>
    /// Toolbar màn Lịch [SD1 §3.1]: ‹ · nhãn khoảng có menu · › · "Hôm nay (T)" · tab zoom · menu bắt lưới · giãn · ô tìm id đợt.
    /// Lớp này chỉ ĐỔ nội dung vào phần tử <c>calendar-toolbar</c> của UXML, không tự là control mới — nhờ vậy UXML giữ nguyên
    /// hình dạng màn và test Q theo tên element như mọi phần khác.
    /// </summary>
    internal sealed class CalendarToolbar
    {
        /// <summary>Icon ⋮ của menu tràn — cùng icon với menu ⋮ của section header [FD §2.12].</summary>
        private const string OverflowIconName = "_Menu";

        private const int OverflowIconSize = 14;

        /// <summary>Dấu ▾ của nhãn menu zoom ("3 tuần ▾") — <see cref="LiveOpsChevron"/> vẽ tam giác, nhãn chỉ mang chữ.</summary>
        private const int ZoomMenuChoiceCount = 3;

        private readonly Toolbar _host;
        private readonly LiveOpsHubFormat _format;
        private readonly ToolbarButton _todayButton;
        private readonly ToolbarMenu _rangeMenu;
        private readonly ToolbarMenu _snapMenu;
        private readonly LiveOpsTabStrip _zoomTabs;
        private readonly ToolbarSearchField _search;
        private readonly Label _hiddenLanesChip;
        private readonly ToolbarToggle _listToggle;
        private readonly ToolbarToggle _compareToggle;
        private readonly Label _compareDisabledReason;
        private readonly ToolbarMenu _zoomMenu;
        private readonly ToolbarMenu _overflowMenu;

        private bool _isNarrow;
        private bool _isLegendVisible;
        private bool _rangeContainsToday;
        private bool _hasRange;
        private DateTime _rangeStartUtc;
        private DateTime _rangeEndUtc;

        public CalendarToolbar(Toolbar host, LiveOpsHubFormat format)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _format = format ?? throw new ArgumentNullException(nameof(format));

            // "Danh sách" tách khỏi cụm điều hướng và đứng đầu toolbar [SD1 §3.1] — nó đổi BỐ CỤC màn, không đổi khoảng đang xem.
            _listToggle = new ToolbarToggle { text = LiveOpsHubStrings.CalendarDepthListToggle };
            _listToggle.AddToClassList(LiveOpsHubClassNames.CalendarDepthListToggle);
            _listToggle.RegisterValueChangedCallback(change => ListPaneToggled?.Invoke(change.newValue));
            _host.Add(_listToggle);

            VisualElement navigationGroup = new VisualElement();
            navigationGroup.AddToClassList(LiveOpsHubClassNames.CalendarToolbarGroup);
            ToolbarButton previousButton = new ToolbarButton(() => RangeStepRequested?.Invoke(-1))
            {
                tooltip = LiveOpsHubStrings.CalendarRangePreviousTooltip,
            };
            previousButton.Add(new LiveOpsChevron(LiveOpsChevron.ChevronDirection.Left));
            navigationGroup.Add(previousButton);

            _rangeMenu = new ToolbarMenu();
            _rangeMenu.menu.AppendAction(LiveOpsHubStrings.CalendarRangeMenuEarlier, _ => RangeStepRequested?.Invoke(-1));
            _rangeMenu.menu.AppendAction(LiveOpsHubStrings.CalendarRangeMenuLater, _ => RangeStepRequested?.Invoke(1));
            _rangeMenu.menu.AppendAction(LiveOpsHubStrings.CalendarRangeMenuPickStart, _ => PickRangeStartRequested?.Invoke());
            navigationGroup.Add(_rangeMenu);

            ToolbarButton nextButton = new ToolbarButton(() => RangeStepRequested?.Invoke(1))
            {
                tooltip = LiveOpsHubStrings.CalendarRangeNextTooltip,
            };
            nextButton.Add(new LiveOpsChevron(LiveOpsChevron.ChevronDirection.Right));
            navigationGroup.Add(nextButton);

            _todayButton = new ToolbarButton(() => TodayRequested?.Invoke()) { text = LiveOpsHubStrings.CalendarTodayButton };
            _todayButton.AddToClassList(LiveOpsHubClassNames.CalendarDepthTodayButton);
            navigationGroup.Add(_todayButton);
            _host.Add(navigationGroup);

            _zoomTabs = new LiveOpsTabStrip { Choices = LiveOpsHubStrings.CalendarZoomChoices };
            _zoomTabs.AddToClassList(LiveOpsHubClassNames.CalendarDepthZoomTabs);
            _zoomTabs.SelectedIndexChanged += index => ZoomChanged?.Invoke(ZoomOf(index));
            _host.Add(_zoomTabs);

            // Cửa sổ hẹp: ba tab zoom thu thành MỘT menu "3 tuần ▾" (8.8 [FD §4.2]). Cả hai luôn có trong cây, USS chọn cái nào
            // hiện theo class --narrow của root — đổi cây theo bề rộng sẽ mất lựa chọn đang có mỗi lần người dùng kéo cửa sổ.
            _zoomMenu = new ToolbarMenu();
            _zoomMenu.AddToClassList(LiveOpsHubClassNames.CalendarDepthZoomMenu);
            for (int index = 0; index < ZoomMenuChoiceCount; index++) AppendZoomChoice(index);
            _host.Add(_zoomMenu);

            _snapMenu = new ToolbarMenu();
            AppendSnapChoice(CalendarSnapMode.Automatic, LiveOpsHubStrings.CalendarSnapAuto);
            AppendSnapChoice(CalendarSnapMode.FifteenMinutes, LiveOpsHubStrings.CalendarSnapFifteenMinutes);
            AppendSnapChoice(CalendarSnapMode.Hour, LiveOpsHubStrings.CalendarSnapHour);
            AppendSnapChoice(CalendarSnapMode.Day, LiveOpsHubStrings.CalendarSnapDay);
            AppendSnapChoice(CalendarSnapMode.Off, LiveOpsHubStrings.CalendarSnapOff);
            _host.Add(_snapMenu);

            VisualElement spacer = new VisualElement();
            spacer.AddToClassList(LiveOpsHubClassNames.CalendarToolbarSpacer);
            _host.Add(spacer);

            // "So với đã đăng (n)" [SD1 §3.4]: pane so THAY CHỖ inspector, nên nút nằm sau phần giãn, cạnh ô tìm.
            _compareToggle = new ToolbarToggle();
            _compareToggle.AddToClassList(LiveOpsHubClassNames.CalendarDepthCompareToggle);
            _compareToggle.RegisterValueChangedCallback(change => ComparePaneToggled?.Invoke(change.newValue));
            _host.Add(_compareToggle);

            // SPIKE-B SP-3: lý do nút bị khoá LUÔN in thành chữ cạnh nút, tooltip chỉ là bản phụ.
            _compareDisabledReason = new Label();
            _compareDisabledReason.AddToClassList(LiveOpsHubClassNames.CalendarDepthDisabledReason);
            _compareDisabledReason.AddToClassList(LiveOpsHubClassNames.CalendarHidden);
            _host.Add(_compareDisabledReason);

            _hiddenLanesChip = new Label();
            _hiddenLanesChip.AddToClassList(LiveOpsHubClassNames.CalendarHiddenLanesChip);
            _host.Add(_hiddenLanesChip);
            _hiddenLanesChip.AddToClassList(LiveOpsHubClassNames.CalendarHidden);

            _search = new ToolbarSearchField();
            _search.AddToClassList(LiveOpsHubClassNames.CalendarSearch);
            // ToolbarSearchField không có chữ dẫn sẵn ở 2022.3; ô nhập là TextField con nên gắn vào đó. Không tìm thấy thì bỏ
            // qua — ô tìm thiếu chữ dẫn vẫn dùng được, ném ở đây sẽ làm sập cả màn.
            TextField searchInput = _search.Q<TextField>();
            if (searchInput != null) LiveOpsPlaceholder.Attach(searchInput, LiveOpsHubStrings.CalendarSearchPlaceholder);
            _search.RegisterValueChangedCallback(change => SearchChanged?.Invoke(change.newValue ?? string.Empty));
            // Enter = đợt kế tiếp khớp chuỗi đang gõ (7.3). Không có nó thì ô tìm chỉ tới được đợt khớp ĐẦU TIÊN và mọi đợt sau
            // trùng tiền tố id trở thành không tìm ra bằng bàn phím.
            _search.RegisterCallback<KeyDownEvent>(OnSearchKeyDown);
            _host.Add(_search);

            // Menu ⋮ của toolbar: chỗ ở của việc ít dùng khi cửa sổ hẹp ("nút ít dùng + Chú giải vào ⋮", 8.8). Trạng thái từng mục
            // tính LẠI mỗi lần mở menu (actionStatusCallback) nên dấu tích và mục khoá luôn khớp màn, không phải bản chụp lúc dựng.
            _overflowMenu = new ToolbarMenu { tooltip = LiveOpsHubStrings.CalendarDepthOverflowMenuTooltip };
            _overflowMenu.AddToClassList(LiveOpsHubClassNames.CalendarDepthOverflowMenu);
            _overflowMenu.Add(LiveOpsHubIcons.CreateImage(OverflowIconName, OverflowIconSize));
            _overflowMenu.menu.AppendAction(LiveOpsHubStrings.CalendarDepthListToggle,
                _ => ListPaneToggled?.Invoke(!_listToggle.value),
                _ => _listToggle.value ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            _overflowMenu.menu.AppendAction(LiveOpsHubStrings.CalendarTodayButton, _ => TodayRequested?.Invoke(),
                _ => _rangeContainsToday ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);
            _overflowMenu.menu.AppendAction(LiveOpsHubStrings.CalendarDepthLegendToggle, _ => SetLegendVisible(!_isLegendVisible),
                _ => _isLegendVisible ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            _host.Add(_overflowMenu);

            SetSnapMode(CalendarSnapMode.Automatic);
            SetCompareState(false, false, 0);
        }

        /// <summary>−1 = khoảng trước, +1 = khoảng sau.</summary>
        public event Action<int> RangeStepRequested;

        public event Action PickRangeStartRequested;
        public event Action TodayRequested;
        public event Action<LiveOpsTimelineZoom> ZoomChanged;
        public event Action<CalendarSnapMode> SnapModeChanged;
        public event Action<string> SearchChanged;

        /// <summary>Bật/tắt pane Danh sách bên trái split.</summary>
        public event Action<bool> ListPaneToggled;

        /// <summary>Bật/tắt pane "So với đã đăng" — loại trừ với inspector đợt.</summary>
        public event Action<bool> ComparePaneToggled;

        /// <summary>Enter trong ô tìm: nhảy tới đợt khớp KẾ TIẾP, vòng lại đầu danh sách khi hết.</summary>
        public event Action<string> SearchSubmitted;

        /// <summary>Mục "Chú giải" của menu ⋮ — dải chú giải bị USS ẩn ở cửa sổ hẹp, mục này bật lại khi cần tra ký hiệu.</summary>
        public event Action<bool> LegendVisibilityChanged;

        public CalendarSnapMode SnapMode { get; private set; } = CalendarSnapMode.Automatic;

        public LiveOpsTimelineZoom Zoom => ZoomOf(_zoomTabs.SelectedIndex);

        internal ToolbarSearchField SearchField => _search;
        internal ToolbarButton TodayButton => _todayButton;
        internal ToolbarMenu RangeMenu => _rangeMenu;
        internal ToolbarMenu SnapMenu => _snapMenu;
        internal LiveOpsTabStrip ZoomTabs => _zoomTabs;
        internal Label HiddenLanesChip => _hiddenLanesChip;
        internal ToolbarToggle ListToggle => _listToggle;
        internal ToolbarToggle CompareToggle => _compareToggle;
        internal Label CompareDisabledReason => _compareDisabledReason;
        internal ToolbarMenu ZoomMenu => _zoomMenu;
        internal ToolbarMenu OverflowMenu => _overflowMenu;

        /// <summary>Toolbar đang ở dạng rút gọn (cửa sổ dưới 900px) — màn bơm xuống từ class <c>--narrow</c> của root.</summary>
        internal bool IsNarrow => _isNarrow;

        internal bool IsLegendVisible => _isLegendVisible;

        /// <summary>Nhãn khoảng đang xem; "Hôm nay" tắt kèm lý do khi khung đã chứa hôm nay (7.0 — lý do luôn in thành chữ).</summary>
        public void SetRange(DateTime rangeStartUtc, DateTime rangeEndUtc, DateTime nowUtc)
        {
            _hasRange = true;
            _rangeStartUtc = rangeStartUtc;
            _rangeEndUtc = rangeEndUtc;
            _rangeContainsToday = nowUtc >= rangeStartUtc && nowUtc < rangeEndUtc;
            ApplyRangeLabel();
            _todayButton.SetEnabled(!_rangeContainsToday);
            _todayButton.tooltip = _rangeContainsToday ? LiveOpsHubStrings.CalendarTodayDisabledReason : string.Empty;
        }

        /// <summary>
        /// Dạng rút gọn của toolbar (8.8): tab zoom nhường chỗ cho menu, nút ít dùng vào ⋮, ô tìm 120px — phần NHÌN THẤY do USS
        /// lo theo class <c>--narrow</c> của root; C# chỉ cần biết để bỏ NĂM khỏi nhãn khoảng ("6/9 – 27/9"), thứ USS không làm được.
        /// </summary>
        public void SetNarrow(bool isNarrow)
        {
            if (_isNarrow == isNarrow) return;
            _isNarrow = isNarrow;
            ApplyRangeLabel();
        }

        /// <summary>Bật/tắt dải chú giải từ menu ⋮; phát sự kiện để màn gắn class lên chính dải đó.</summary>
        public void SetLegendVisible(bool isVisible)
        {
            if (_isLegendVisible == isVisible) return;
            _isLegendVisible = isVisible;
            LegendVisibilityChanged?.Invoke(isVisible);
        }

        /// <summary>Nhãn khoảng: có năm ở cửa sổ rộng, bỏ năm khi hẹp — "6/9/2026 – 27/9/2026" bị cắt mất vế sau ở 820px.</summary>
        private void ApplyRangeLabel()
        {
            if (!_hasRange) return;
            _rangeMenu.text = _isNarrow
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRangeLabelFormat,
                    LiveOpsHubFormat.DayMonthText(_rangeStartUtc), LiveOpsHubFormat.DayMonthText(_rangeEndUtc))
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRangeLabelFormat,
                    _format.DateWithYear(_rangeStartUtc), _format.DateWithYear(_rangeEndUtc));
        }

        public void SetZoomWithoutNotify(LiveOpsTimelineZoom zoom)
        {
            _zoomTabs.SetSelectedIndexWithoutNotify(IndexOf(zoom));
            _zoomMenu.text = ZoomChoiceAt(IndexOf(zoom));
        }

        /// <summary>(nợ D-3(c)) Lựa chọn đi thẳng xuống <c>LiveOpsTimelineDragController.SnapStep</c>, nên nhãn nói đúng việc đang làm.</summary>
        public void SetSnapMode(CalendarSnapMode mode)
        {
            SnapMode = mode;
            _snapMenu.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarSnapMenuFormat, LabelOf(mode));
        }

        /// <summary>Trạng thái nút "Danh sách" khi khôi phục view state — không phát sự kiện để khỏi vẽ lại hai lần.</summary>
        public void SetListPaneOpenWithoutNotify(bool isOpen)
        {
            _listToggle.SetValueWithoutNotify(isOpen);
        }

        /// <summary>
        /// Nút "So với đã đăng (n)": số là số thay đổi so với bản so đang chọn. Chưa đăng lần nào thì nút TẮT và lý do in thành
        /// chữ ("Chưa có dấu đã đăng") chứ không chỉ nằm trong tooltip (SPIKE-B SP-3).
        /// </summary>
        public void SetCompareState(bool isOpen, bool isAvailable, int changeCount)
        {
            _compareToggle.SetValueWithoutNotify(isOpen);
            _compareToggle.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthCompareToggleFormat,
                changeCount);
            _compareToggle.SetEnabled(isAvailable);
            _compareToggle.tooltip = isAvailable ? string.Empty : LiveOpsHubStrings.CalendarDepthCompareUnavailableReason;
            _compareDisabledReason.text = isAvailable ? string.Empty : LiveOpsHubStrings.CalendarDepthCompareUnavailableReason;
            _compareDisabledReason.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, isAvailable);
        }

        /// <summary>Chip chỉ hiện khi thật sự có làn ẩn — không có làn ẩn thì không chiếm chỗ trên toolbar.</summary>
        public void SetHiddenLaneCount(int hiddenLaneCount)
        {
            _hiddenLanesChip.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, hiddenLaneCount <= 0);
            if (hiddenLaneCount <= 0) return;
            _hiddenLanesChip.text = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.CalendarHiddenLanesChipFormat),
                hiddenLaneCount);
        }

        /// <summary>
        /// Test gọi thẳng nhánh phím của ô tìm. Không gửi <c>KeyDownEvent</c> qua <c>SendEvent</c>: ở 2022.3 phím được dispatch
        /// theo ELEMENT ĐANG FOCUS chứ không theo target đã gán, nên cùng một test xanh ở 6000.6 và đỏ ở 2022.3.
        /// </summary>
        internal void HandleSearchKeyDownForTest(KeyDownEvent keyEvent)
        {
            OnSearchKeyDown(keyEvent);
        }

        private void OnSearchKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode != KeyCode.Return && keyEvent.keyCode != KeyCode.KeypadEnter) return;
            keyEvent.StopPropagation();
            SearchSubmitted?.Invoke(_search.value ?? string.Empty);
        }

        private void AppendZoomChoice(int choiceIndex)
        {
            _zoomMenu.menu.AppendAction(ZoomChoiceAt(choiceIndex), _ =>
            {
                _zoomTabs.SetSelectedIndexWithoutNotify(choiceIndex);
                _zoomMenu.text = ZoomChoiceAt(choiceIndex);
                ZoomChanged?.Invoke(ZoomOf(choiceIndex));
            }, _ => _zoomTabs.SelectedIndex == choiceIndex ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
        }

        /// <summary>Nhãn một lựa chọn zoom, tách từ CÙNG chuỗi "Ngày|3 tuần|Tháng" mà tab strip đọc — hai nơi không được lệch chữ.</summary>
        private static string ZoomChoiceAt(int choiceIndex)
        {
            string[] choices = LiveOpsHubStrings.CalendarZoomChoices.Split('|');
            return choiceIndex >= 0 && choiceIndex < choices.Length ? choices[choiceIndex] : string.Empty;
        }

        private void AppendSnapChoice(CalendarSnapMode mode, string label)
        {
            _snapMenu.menu.AppendAction(label, _ =>
            {
                SetSnapMode(mode);
                SnapModeChanged?.Invoke(mode);
            });
        }

        private static string LabelOf(CalendarSnapMode mode)
        {
            switch (mode)
            {
                case CalendarSnapMode.FifteenMinutes: return LiveOpsHubStrings.CalendarSnapFifteenMinutes;
                case CalendarSnapMode.Hour: return LiveOpsHubStrings.CalendarSnapHour;
                case CalendarSnapMode.Day: return LiveOpsHubStrings.CalendarSnapDay;
                case CalendarSnapMode.Off: return LiveOpsHubStrings.CalendarSnapOff;
                default: return LiveOpsHubStrings.CalendarSnapAuto;
            }
        }

        private static LiveOpsTimelineZoom ZoomOf(int index)
        {
            switch (index)
            {
                case 0: return LiveOpsTimelineZoom.Day;
                case 2: return LiveOpsTimelineZoom.Month;
                default: return LiveOpsTimelineZoom.ThreeWeeks;
            }
        }

        private static int IndexOf(LiveOpsTimelineZoom zoom)
        {
            switch (zoom)
            {
                case LiveOpsTimelineZoom.Day: return 0;
                case LiveOpsTimelineZoom.Month: return 2;
                default: return 1;
            }
        }
    }
}
