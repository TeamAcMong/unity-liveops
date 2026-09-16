using System;
using System.Globalization;
using UnityEditor.UIElements;
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
    // INTERIM(G-CALENDAR-DEPTH): W4 chưa có toggle "Danh sách" và "So với đã đăng (n)" (mục 12 I-5). Hai nút KHÔNG hiện (không
    // vẽ disabled): nút trỏ tới pane chưa dựng thì bấm vào không có gì xảy ra, khó hiểu hơn là chưa có nút.
    internal sealed class CalendarToolbar
    {
        private readonly Toolbar _host;
        private readonly LiveOpsHubFormat _format;
        private readonly ToolbarButton _todayButton;
        private readonly ToolbarMenu _rangeMenu;
        private readonly ToolbarMenu _snapMenu;
        private readonly LiveOpsTabStrip _zoomTabs;
        private readonly ToolbarSearchField _search;
        private readonly Label _hiddenLanesChip;

        public CalendarToolbar(Toolbar host, LiveOpsHubFormat format)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _format = format ?? throw new ArgumentNullException(nameof(format));

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
            navigationGroup.Add(_todayButton);
            _host.Add(navigationGroup);

            _zoomTabs = new LiveOpsTabStrip { Choices = LiveOpsHubStrings.CalendarZoomChoices };
            _zoomTabs.SelectedIndexChanged += index => ZoomChanged?.Invoke(ZoomOf(index));
            _host.Add(_zoomTabs);

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

            _hiddenLanesChip = new Label();
            _hiddenLanesChip.AddToClassList(LiveOpsHubClassNames.CalendarHiddenLanesChip);
            _host.Add(_hiddenLanesChip);
            _hiddenLanesChip.AddToClassList(LiveOpsHubClassNames.CalendarHidden);

            _search = new ToolbarSearchField();
            _search.AddToClassList(LiveOpsHubClassNames.CalendarSearch);
            _search.RegisterValueChangedCallback(change => SearchChanged?.Invoke(change.newValue ?? string.Empty));
            _host.Add(_search);

            SetSnapMode(CalendarSnapMode.Automatic);
        }

        /// <summary>−1 = khoảng trước, +1 = khoảng sau.</summary>
        public event Action<int> RangeStepRequested;

        public event Action PickRangeStartRequested;
        public event Action TodayRequested;
        public event Action<LiveOpsTimelineZoom> ZoomChanged;
        public event Action<CalendarSnapMode> SnapModeChanged;
        public event Action<string> SearchChanged;

        public CalendarSnapMode SnapMode { get; private set; } = CalendarSnapMode.Automatic;

        public LiveOpsTimelineZoom Zoom => ZoomOf(_zoomTabs.SelectedIndex);

        internal ToolbarSearchField SearchField => _search;
        internal ToolbarButton TodayButton => _todayButton;
        internal ToolbarMenu RangeMenu => _rangeMenu;
        internal ToolbarMenu SnapMenu => _snapMenu;
        internal LiveOpsTabStrip ZoomTabs => _zoomTabs;
        internal Label HiddenLanesChip => _hiddenLanesChip;

        /// <summary>Nhãn khoảng đang xem; "Hôm nay" tắt kèm lý do khi khung đã chứa hôm nay (7.0 — lý do luôn in thành chữ).</summary>
        public void SetRange(DateTime rangeStartUtc, DateTime rangeEndUtc, DateTime nowUtc)
        {
            _rangeMenu.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRangeLabelFormat,
                _format.DateWithYear(rangeStartUtc), _format.DateWithYear(rangeEndUtc));
            bool containsToday = nowUtc >= rangeStartUtc && nowUtc < rangeEndUtc;
            _todayButton.SetEnabled(!containsToday);
            _todayButton.tooltip = containsToday ? LiveOpsHubStrings.CalendarTodayDisabledReason : string.Empty;
        }

        public void SetZoomWithoutNotify(LiveOpsTimelineZoom zoom)
        {
            _zoomTabs.SetSelectedIndexWithoutNotify(IndexOf(zoom));
        }

        public void SetSnapMode(CalendarSnapMode mode)
        {
            SnapMode = mode;
            _snapMenu.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarSnapMenuFormat, LabelOf(mode));
        }

        /// <summary>Chip chỉ hiện khi thật sự có làn ẩn — không có làn ẩn thì không chiếm chỗ trên toolbar.</summary>
        public void SetHiddenLaneCount(int hiddenLaneCount)
        {
            _hiddenLanesChip.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, hiddenLaneCount <= 0);
            if (hiddenLaneCount <= 0) return;
            _hiddenLanesChip.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarHiddenLanesChipFormat,
                hiddenLaneCount);
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
