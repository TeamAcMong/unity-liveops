using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Popover Thêm đợt ba bước [SD1 §3.11]. Dựng trên <see cref="LiveOpsPopoverContent"/> — lớp gốc của MỌI popover hub — nên
    /// cửa sổ đúng 320×N, Esc đóng, focus đầu vào ô lọc và không bao giờ có hai popover cùng mở. Cây popover là panel RIÊNG nên
    /// sheet của màn Lịch phải gắn lại ở đây: không có nó thì <c>liveops-hub-calendar--hidden</c> không tới nơi và ba bước hiện
    /// cùng lúc (lỗi đã thấy trên ảnh Hình 13b).
    /// <para>
    /// Enter = bước tiếp; ở bước xem lại có chồng giờ Enter là "Quay lại sửa giờ", KHÔNG phải "Vẫn thêm" (SP-2 (c): nút phá huỷ
    /// không bao giờ là nút mặc định). Escape do lớp gốc lo.
    /// </para>
    /// </summary>
    internal sealed class AddEventPopover : LiveOpsPopoverContent
    {
        /// <summary>Bề rộng popover theo mockup; chiều cao mockup 236 — nội dung từng bước vẫn cuộn được trong đó.</summary>
        internal const float PopoverWidth = 320f;

        internal const float PopoverHeight = 236f;

        private readonly LiveOpsHubServices _services;
        private readonly Action<AddEventFlowModel> _submit;
        private readonly ILiveOpsHubLayoutLoader _layoutLoader;

        private AddEventFlowModel _flow;
        private VisualElement _root;
        private Label _headerTitle;
        private VisualElement _stepDots;
        private VisualElement _stepType;
        private VisualElement _stepTimes;
        private VisualElement _stepReview;
        private TextField _typeFilter;
        private ScrollView _typeList;
        private VisualElement _buttons;

        public AddEventPopover(LiveOpsHubServices services, AddEventFlowModel flow, Action<AddEventFlowModel> submit)
            : this(services, flow, submit, null)
        {
        }

        internal AddEventPopover(LiveOpsHubServices services, AddEventFlowModel flow, Action<AddEventFlowModel> submit,
            ILiveOpsHubLayoutLoader layoutLoader)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _flow = flow ?? throw new ArgumentNullException(nameof(flow));
            _submit = submit ?? throw new ArgumentNullException(nameof(submit));
            _layoutLoader = layoutLoader ?? services.LayoutLoader;
        }

        public AddEventFlowModel Flow => _flow;

        internal VisualElement Root => _root;

        protected override Vector2 PopoverSize
        {
            get { return new Vector2(PopoverWidth, PopoverHeight); }
        }

        protected override Focusable InitialFocus
        {
            get { return _typeFilter; }
        }

        protected override VisualElement BuildContent()
        {
            VisualTreeAsset layout = _layoutLoader.LoadVisualTree(LiveOpsHubPaths.AddEventPopoverUxml);
            _root = new VisualElement();
            if (layout == null) return _root;
            layout.CloneTree(_root);

            // Panel riêng: PopoverSheets của lớp gốc chỉ có theme + components + feedback + motion. Class riêng của màn Lịch
            // (bước ẩn/hiện, step-dot, hàng loại, hàng nút) sống ở CalendarSection.uss nên gắn thêm chính sheet đó.
            StyleSheet calendarSheet = _layoutLoader.LoadStyleSheet(LiveOpsHubPaths.CalendarSectionUss);
            if (calendarSheet != null) _root.styleSheets.Add(calendarSheet);

            _headerTitle = _root.Q<Label>(LiveOpsHubPaths.AddEventPopoverElementNames.HeaderTitle);
            _stepDots = _root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.StepDots);
            _stepType = _root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.StepType);
            _stepTimes = _root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.StepTimes);
            _stepReview = _root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.StepReview);
            _typeFilter = _root.Q<TextField>(LiveOpsHubPaths.AddEventPopoverElementNames.TypeFilter);
            _typeList = _root.Q<ScrollView>(LiveOpsHubPaths.AddEventPopoverElementNames.TypeList);
            _buttons = _root.Q(LiveOpsHubPaths.AddEventPopoverElementNames.Buttons);

            if (_typeFilter != null)
            {
                LiveOpsPlaceholder.Attach(_typeFilter, LiveOpsHubStrings.CalendarAddTypeFilterPlaceholder);
                _typeFilter.RegisterValueChangedCallback(_ => RefreshTypeList());
            }
            // Escape đã có ở lớp gốc; Enter là đường riêng của popover này nên đăng ký trên chính cây nội dung.
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            Refresh();
            return _root;
        }

        internal void Refresh()
        {
            if (_root == null) return;
            RefreshHeader();
            SetStepVisible(_stepType, _flow.Step == AddEventFlowModel.StepChooseType);
            SetStepVisible(_stepTimes, _flow.Step == AddEventFlowModel.StepChooseTimes);
            SetStepVisible(_stepReview, _flow.Step == AddEventFlowModel.StepReview);
            RefreshTypeList();
            RefreshTimes();
            RefreshReview();
            RefreshButtons();
        }

        private void RefreshHeader()
        {
            if (_headerTitle != null) _headerTitle.text = HeaderTitleText();
            if (_stepDots == null) return;
            _stepDots.Clear();
            for (int step = AddEventFlowModel.StepChooseType; step <= AddEventFlowModel.StepReview; step++)
            {
                Label dot = new Label(step.ToString(CultureInfo.InvariantCulture));
                dot.AddToClassList(LiveOpsHubClassNames.CalendarStepDot);
                if (step == _flow.Step) dot.AddToClassList(LiveOpsHubClassNames.CalendarStepDotActive);
                _stepDots.Add(dot);
            }
        }

        private string HeaderTitleText()
        {
            if (_flow.Step == AddEventFlowModel.StepReview) return LiveOpsHubStrings.CalendarAddReviewTitle;
            if (_flow.Step == AddEventFlowModel.StepChooseTimes)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarAddStepTypeTitleFormat, _flow.EventType);
            }
            return LiveOpsHubStrings.CalendarAddEventButton;
        }

        private void RefreshTypeList()
        {
            if (_typeList == null || _flow.Step != AddEventFlowModel.StepChooseType) return;
            string filter = _typeFilter == null ? string.Empty : _typeFilter.value;
            AddEventFlowModel pointed = _flow.PointingAtFilteredType(filter);
            if (!ReferenceEquals(pointed, _flow))
            {
                _flow = pointed;
                // Trỏ lại đổi tiêu đề và nút "Tiếp"; vẽ đúng hai phần đó thay vì gọi Refresh() — Refresh() gọi lại chính hàm này.
                RefreshHeader();
                RefreshButtons();
            }
            _typeList.Clear();
            IReadOnlyList<AddEventTypeChoice> choices = _flow.TypeChoices(filter);
            for (int index = 0; index < choices.Count; index++) _typeList.Add(BuildTypeRow(choices[index]));
        }

        private VisualElement BuildTypeRow(AddEventTypeChoice choice)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarTypeRow);
            if (!choice.IsEnabled) row.AddToClassList(LiveOpsHubClassNames.CalendarTypeRowDisabled);
            if (string.Equals(choice.TypeId, _flow.EventType, StringComparison.Ordinal))
            {
                row.AddToClassList(LiveOpsHubClassNames.CalendarTypeRowActive);
            }

            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            LiveOpsHubStyle.SetEventColor(swatch, choice.ColorSlot);
            row.Add(swatch);

            Label id = new Label(choice.TypeId);
            id.AddToClassList(LiveOpsHubClassNames.Mono);
            row.Add(id);

            Label tag = new Label(choice.TagText);
            tag.AddToClassList(LiveOpsHubClassNames.CalendarTypeRowTag);
            row.Add(tag);

            if (!choice.IsEnabled) return row;
            // Bấm một hàng chỉ ĐANG TRỎ tới loại đó: bước sau mở bằng Enter hoặc nút "Tiếp" [SD1 §3.11 bước 1].
            row.RegisterCallback<ClickEvent>(_ =>
            {
                _flow = _flow.WithType(choice.TypeId);
                Refresh();
            });
            return row;
        }

        private void RefreshTimes()
        {
            if (_stepTimes == null || _flow.Step != AddEventFlowModel.StepChooseTimes) return;
            _stepTimes.Clear();

            LiveOpsUtcDateTimeField start = new LiveOpsUtcDateTimeField(LiveOpsHubStrings.CalendarFieldStartLabel);
            start.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            start.SetDeviceOffset(_services.TimeZone.DeviceOffsetAt(_services.Clock.UtcNow));
            start.SetRawTextWithoutNotify(_flow.StartDateText, _flow.StartTimeText);
            // Một đường chốt duy nhất của ô giờ: bản trước nghe RawTextCommitted (chỉ bắn khi chữ HỎNG) nên gõ ngày/giờ HỢP LỆ
            // rồi Enter/Tab là _flow không đổi — bước 3 hiện giờ cũ và đợt được thêm với giờ cũ (UJ-03/UX-04).
            start.TextCommitted += (dateText, timeText) =>
            {
                _flow = _flow.WithTimes(dateText, timeText, _flow.DurationHours);
                Refresh();
            };
            _stepTimes.Add(start);

            _stepTimes.Add(BuildDurationRow());
            _stepTimes.Add(BuildEndRow());
        }

        /// <summary>Hàng "Dài": ô số · đơn vị "giờ" · ghi chú "= 3 ngày" [SD1 §3.11 bước 2] — số giờ trần không đọc ra được độ dài.</summary>
        private VisualElement BuildDurationRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            row.AddToClassList(LiveOpsHubClassNames.CalendarInspectorRow);
            IntegerField duration = new IntegerField(LiveOpsHubStrings.CalendarFieldDurationLabel)
            {
                value = _flow.DurationHours,
                isDelayed = true,
            };
            duration.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            duration.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldNumber);
            duration.RegisterValueChangedCallback(change =>
            {
                _flow = _flow.WithTimes(_flow.StartDateText, _flow.StartTimeText, change.newValue);
                Refresh();
            });
            row.Add(duration);
            row.Add(new Label(LiveOpsHubStrings.CalendarDurationUnitLabel));
            row.Add(BuildDurationNote(_flow.DurationHours, _services.Format));
            return row;
        }

        /// <summary>"= 3 ngày" — dùng chung với inspector để hai chỗ không viết hai kiểu cùng một con số.</summary>
        internal static Label BuildDurationNote(int durationHours, LiveOpsHubFormat format)
        {
            string text = durationHours > 0
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDurationNoteFormat,
                    format.Duration(TimeSpan.FromHours(durationHours), false))
                : string.Empty;
            Label note = new Label(text);
            note.AddToClassList(LiveOpsHubClassNames.CalendarFieldSubline);
            note.AddToClassList(LiveOpsHubClassNames.CalendarFieldNoteInline);
            return note;
        }

        /// <summary>Hàng "Kết thúc": có NHÃN như mọi field khác, giờ tự tính, dòng phụ "UTC · tự tính" kèm giờ máy.</summary>
        private VisualElement BuildEndRow()
        {
            VisualElement wrapper = new VisualElement();
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            row.AddToClassList(LiveOpsHubClassNames.CalendarInspectorRow);
            Label endLabel = new Label(LiveOpsHubStrings.CalendarFieldEndLabel);
            endLabel.AddToClassList(LiveOpsHubClassNames.CalendarInspectorLabel);
            row.Add(endLabel);
            Label endValue = new Label(EndText());
            endValue.AddToClassList(LiveOpsHubClassNames.Mono);
            endValue.AddToClassList(LiveOpsHubClassNames.CalendarInspectorValue);
            row.Add(endValue);
            wrapper.Add(row);

            Label endNote = new Label(EndNoteText());
            endNote.AddToClassList(LiveOpsHubClassNames.CalendarFieldSubline);
            endNote.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            wrapper.Add(endNote);
            return wrapper;
        }

        private string EndText()
        {
            DateTime? endUtc = _flow.EndUtc;
            return endUtc == null ? string.Empty : _services.Format.ShortDateTime(endUtc.Value);
        }

        /// <summary>"UTC · tự tính" + giờ máy của chính mốc đó — ô giờ chỉ nhận UTC nên dòng phụ là chỗ duy nhất nói giờ máy.</summary>
        private string EndNoteText()
        {
            DateTime? endUtc = _flow.EndUtc;
            if (endUtc == null) return LiveOpsHubStrings.CalendarAddEndAutoNote;
            return LiveOpsHubStrings.CalendarAddEndAutoNote + LiveOpsHubStrings.ShellRailPartSeparator
                   + _services.Format.DeviceTimeLine(endUtc.Value);
        }

        private void RefreshReview()
        {
            if (_stepReview == null || _flow.Step != AddEventFlowModel.StepReview) return;
            _stepReview.Clear();

            TextField id = new TextField(LiveOpsHubStrings.CalendarFieldIdLabel) { value = _flow.SuggestedEventId, isDelayed = true };
            id.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            id.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldText);
            id.AddToClassList(LiveOpsHubClassNames.Mono);
            id.RegisterValueChangedCallback(change =>
            {
                _flow = _flow.WithEventId(change.newValue ?? string.Empty);
                Refresh();
            });
            _stepReview.Add(id);

            VisualElement timesRow = new VisualElement();
            timesRow.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            timesRow.AddToClassList(LiveOpsHubClassNames.CalendarInspectorRow);
            Label timesLabel = new Label(LiveOpsHubStrings.CalendarAddTimesLabel);
            timesLabel.AddToClassList(LiveOpsHubClassNames.CalendarInspectorLabel);
            timesRow.Add(timesLabel);
            Label timesValue = new Label(TimesText());
            timesValue.AddToClassList(LiveOpsHubClassNames.CalendarInspectorValue);
            timesRow.Add(timesValue);
            timesRow.Add(BuildDurationNote(_flow.DurationHours, _services.Format));
            _stepReview.Add(timesRow);

            TextField configKey = new TextField(LiveOpsHubStrings.CalendarFieldConfigKeyLabel)
            {
                value = _flow.ConfigKey,
                isDelayed = true,
            };
            configKey.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            configKey.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldText);
            configKey.AddToClassList(LiveOpsHubClassNames.Mono);
            LiveOpsPlaceholder configKeyHint = LiveOpsPlaceholder.Attach(configKey, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarConfigKeyPlaceholderFormat, _flow.EffectiveConfigKey));
            // [SD1 §3.11]: chữ dẫn NGHIÊNG cỡ nhỏ. Cỡ thường thì "mặc định của loại: hunt_default" dài hơn ô và cụt (V17).
            configKeyHint.AddToClassList(LiveOpsHubClassNames.CalendarFlowHint);
            configKey.RegisterValueChangedCallback(change =>
            {
                _flow = _flow.WithConfigKey(change.newValue ?? string.Empty);
                Refresh();
            });
            _stepReview.Add(configKey);

            Label configKeyNote = new Label(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarAddConfigKeyNoteFormat, _flow.EffectiveConfigKey));
            configKeyNote.AddToClassList(LiveOpsHubClassNames.CalendarFieldSubline);
            configKeyNote.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            _stepReview.Add(configKeyNote);

            _stepReview.Add(BuildQuickCheck());
        }

        private string TimesText()
        {
            DateTime? startUtc = _flow.StartUtc;
            DateTime? endUtc = _flow.EndUtc;
            if (startUtc == null || endUtc == null) return string.Empty;
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarTimeRangeFormat,
                _services.Format.ShortDateTime(startUtc.Value), _services.Format.ShortDateTime(endUtc.Value));
        }

        /// <summary>Kiểm nhanh chỉ xét làn của loại đang chọn — tag nói rõ phạm vi, câu lỗi lấy từ <see cref="LiveOpsFindingText"/> (V-8).</summary>
        private VisualElement BuildQuickCheck()
        {
            LiveEventCalendarFinding finding = _flow.DropFinding;
            if (finding == null)
            {
                // Họ FILL là màu của DẤU, không phải nền chữ: gắn lên chính Label thì tag thành khối nền quiet, chữ trên nó
                // chỉ còn 1,87:1 và mất luôn dấu Ok ([SD1 §3.11], C9).
                VisualElement okTag = new VisualElement();
                okTag.AddToClassList(LiveOpsHubClassNames.Tag);
                LiveOpsStateMark okMark = new LiveOpsStateMark();
                okMark.SetHealth(HealthState.Ok);
                okTag.Add(okMark);
                Label okText = new Label(LiveOpsHubStrings.CalendarQuickCheckOkTag);
                okText.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
                okTag.Add(okText);
                return okTag;
            }
            VisualElement card = new VisualElement();
            card.AddToClassList(LiveOpsHubClassNames.FindingRow);
            card.AddToClassList(LiveOpsHubClassNames.CalendarFindingCardStacked);
            LiveOpsHubStyle.SetSeverityStripe(card, HealthState.Blocked);
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            Label headline = new Label(LiveOpsFindingText.Headline(finding, _services.Format, document.LatestStamp));
            headline.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            LiveOpsHubStyle.SetStateText(headline, HealthState.Blocked);
            card.Add(headline);
            Label meta = new Label(LiveOpsFindingText.Meta(finding, _services.Format, _services.Clock.UtcNow));
            meta.AddToClassList(LiveOpsHubClassNames.Caption);
            meta.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            card.Add(meta);
            return card;
        }

        private void RefreshButtons()
        {
            if (_buttons == null) return;
            _buttons.Clear();
            // Hàng nút WRAP và gợi ý phím chiếm trọn dòng đầu: cùng dòng với nút thì gợi ý ăn ~110px của 320px, nút chính còn
            // "Thêm hunt-0921 vào lịc" và nút danger còn "Vẫn thêm (game" — đúng câu hậu quả bị giấu (C7).
            _buttons.AddToClassList(LiveOpsHubClassNames.CalendarFlowButtonsWrap);
            Label keyHint = new Label(LiveOpsHubStrings.CalendarAddKeyHint);
            keyHint.AddToClassList(LiveOpsHubClassNames.CalendarFlowKeyLine);
            _buttons.Add(keyHint);

            if (_flow.Step == AddEventFlowModel.StepChooseType)
            {
                _buttons.Add(MakeButton(LiveOpsHubStrings.CalendarAddCancelButton, ClosePopover, false));
            }
            else
            {
                _buttons.Add(MakeButton(BackButtonText(), () =>
                {
                    _flow = _flow.Back();
                    Refresh();
                }, false));
            }

            if (_flow.Step != AddEventFlowModel.StepReview)
            {
                Button next = MakeButton(LiveOpsHubStrings.CalendarAddNextButton, Advance, true);
                next.SetEnabled(_flow.CanAdvance());
                _buttons.Add(next);
                return;
            }
            _buttons.Add(BuildSubmitButton());
        }

        /// <summary>Biến thể chồng giờ: nút chính là "Quay lại sửa giờ" (Enter), còn "Vẫn thêm" là nút danger CHỈ chạy khi click.</summary>
        private string BackButtonText()
        {
            return _flow.Step == AddEventFlowModel.StepReview && _flow.WillBeDropped
                ? LiveOpsHubStrings.CalendarAddBackToTimesButton
                : LiveOpsHubStrings.CalendarAddBackButton;
        }

        private Button BuildSubmitButton()
        {
            if (_flow.WillBeDropped)
            {
                Button anyway = MakeButton(LiveOpsHubStrings.CalendarAddAnywayButton, Submit, false);
                anyway.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
                return anyway;
            }
            string text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarAddSubmitFormat,
                _flow.SuggestedEventId);
            return MakeButton(text, Submit, true);
        }

        private Button MakeButton(string text, Action action, bool isPrimary)
        {
            Button button = new Button(action) { text = text };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            button.AddToClassList(LiveOpsHubClassNames.CalendarFlowButton);
            if (isPrimary) button.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            return button;
        }

        internal void Advance()
        {
            AddEventFlowModel next = _flow.Next();
            if (ReferenceEquals(next, _flow)) return;
            _flow = next;
            Refresh();
        }

        internal void Submit()
        {
            _submit(_flow);
            ClosePopover();
        }

        /// <summary>Enter = bước tiếp; ở bước xem lại có chồng giờ thì Enter là "Quay lại sửa giờ", KHÔNG phải "Vẫn thêm" (7.0 (c)).</summary>
        internal void OnKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode != KeyCode.Return && keyEvent.keyCode != KeyCode.KeypadEnter) return;
            keyEvent.StopPropagation();
            if (_flow.Step != AddEventFlowModel.StepReview)
            {
                Advance();
                return;
            }
            if (_flow.WillBeDropped)
            {
                _flow = _flow.Back();
                Refresh();
                return;
            }
            Submit();
        }

        private static void SetStepVisible(VisualElement step, bool isVisible)
        {
            step?.EnableInClassList(LiveOpsHubClassNames.CalendarHidden, !isVisible);
        }
    }
}
