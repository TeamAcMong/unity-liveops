using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Popover Thêm đợt ba bước [SD1 §3.11] — cửa sổ riêng (<see cref="PopupWindow"/>) nên không bị cắt ở mép hub và tự đóng khi
    /// mất focus. <c>PopupWindowContent</c> KHÔNG tự xử lý Escape, nên <see cref="OnOpen"/> đăng ký <c>KeyDownEvent</c>
    /// TrickleDown trên root của cửa sổ popup: gặp Escape thì chặn lan và đóng — Esc = đóng, không tạo gì.
    /// </summary>
    internal sealed class AddEventPopover : PopupWindowContent
    {
        /// <summary>Bề rộng popover theo mockup; chiều cao để UIElements tự tính theo nội dung từng bước.</summary>
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

        public override Vector2 GetWindowSize()
        {
            return new Vector2(PopoverWidth, PopoverHeight);
        }

        public override void OnGUI(Rect rect)
        {
            // Nội dung là UIElements; OnGUI chỉ để thoả hợp đồng PopupWindowContent.
        }

        public override void OnOpen()
        {
            VisualElement windowRoot = editorWindow == null ? null : editorWindow.rootVisualElement;
            if (windowRoot == null) return;
            windowRoot.Add(Build());
            windowRoot.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _typeFilter?.Focus();
        }

        /// <summary>Dựng cây popover — tách khỏi <see cref="OnOpen"/> để test dựng được mà không mở cửa sổ thật.</summary>
        internal VisualElement Build()
        {
            VisualTreeAsset layout = _layoutLoader.LoadVisualTree(LiveOpsHubPaths.AddEventPopoverUxml);
            _root = new VisualElement();
            _root.AddToClassList(LiveOpsHubClassNames.Root);
            if (layout == null) return _root;
            layout.CloneTree(_root);

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
            _typeList.Clear();
            string filter = _typeFilter == null ? string.Empty : _typeFilter.value;
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
            start.SetDeviceOffset(_services.TimeZone.DeviceOffsetAt(_services.Clock.UtcNow));
            start.SetRawTextWithoutNotify(_flow.StartDateText, _flow.StartTimeText);
            start.RawTextCommitted += (dateText, timeText) =>
            {
                _flow = _flow.WithTimes(dateText, timeText, _flow.DurationHours).Back();
                Refresh();
            };
            _stepTimes.Add(start);

            IntegerField duration = new IntegerField(LiveOpsHubStrings.CalendarFieldDurationLabel)
            {
                value = _flow.DurationHours,
                isDelayed = true,
            };
            duration.RegisterValueChangedCallback(change =>
            {
                _flow = _flow.WithTimes(_flow.StartDateText, _flow.StartTimeText, change.newValue).Back();
                Refresh();
            });
            _stepTimes.Add(duration);

            Label end = new Label(EndText());
            end.AddToClassList(LiveOpsHubClassNames.Mono);
            _stepTimes.Add(end);

            Label endNote = new Label(LiveOpsHubStrings.CalendarAddEndAutoNote);
            endNote.AddToClassList(LiveOpsHubClassNames.CalendarFieldSubline);
            _stepTimes.Add(endNote);
        }

        private string EndText()
        {
            DateTime? endUtc = _flow.EndUtc;
            return endUtc == null ? string.Empty : _services.Format.ShortDateTime(endUtc.Value);
        }

        private void RefreshReview()
        {
            if (_stepReview == null || _flow.Step != AddEventFlowModel.StepReview) return;
            _stepReview.Clear();

            TextField id = new TextField(LiveOpsHubStrings.CalendarFieldIdLabel) { value = _flow.SuggestedEventId, isDelayed = true };
            id.AddToClassList(LiveOpsHubClassNames.Mono);
            id.RegisterValueChangedCallback(change =>
            {
                _flow = _flow.WithEventId(change.newValue ?? string.Empty);
                Refresh();
            });
            _stepReview.Add(id);

            VisualElement timesRow = new VisualElement();
            timesRow.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            timesRow.Add(new Label(LiveOpsHubStrings.CalendarAddTimesLabel));
            timesRow.Add(new Label(TimesText()));
            _stepReview.Add(timesRow);

            TextField configKey = new TextField(LiveOpsHubStrings.CalendarFieldConfigKeyLabel)
            {
                value = _flow.ConfigKey,
                isDelayed = true,
            };
            configKey.AddToClassList(LiveOpsHubClassNames.Mono);
            LiveOpsPlaceholder.Attach(configKey, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarConfigKeyPlaceholderFormat, _flow.EffectiveConfigKey));
            configKey.RegisterValueChangedCallback(change =>
            {
                _flow = _flow.WithConfigKey(change.newValue ?? string.Empty);
                Refresh();
            });
            _stepReview.Add(configKey);

            Label configKeyNote = new Label(string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarAddConfigKeyNoteFormat, _flow.EffectiveConfigKey));
            configKeyNote.AddToClassList(LiveOpsHubClassNames.CalendarFieldSubline);
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
                Label okTag = new Label(LiveOpsHubStrings.CalendarQuickCheckOkTag);
                okTag.AddToClassList(LiveOpsHubClassNames.Tag);
                LiveOpsHubStyle.SetState(okTag, HealthState.Ok);
                return okTag;
            }
            VisualElement card = new VisualElement();
            card.AddToClassList(LiveOpsHubClassNames.FindingRow);
            LiveOpsHubStyle.SetSeverityStripe(card, HealthState.Blocked);
            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            Label headline = new Label(LiveOpsFindingText.Headline(finding, _services.Format, document.LatestStamp));
            LiveOpsHubStyle.SetStateText(headline, HealthState.Blocked);
            card.Add(headline);
            Label meta = new Label(LiveOpsFindingText.Meta(finding, _services.Format, _services.Clock.UtcNow));
            meta.AddToClassList(LiveOpsHubClassNames.Caption);
            card.Add(meta);
            return card;
        }

        private void RefreshButtons()
        {
            if (_buttons == null) return;
            _buttons.Clear();
            Label keyHint = new Label(LiveOpsHubStrings.CalendarAddKeyHint);
            keyHint.AddToClassList(LiveOpsHubClassNames.CalendarFlowKeyHint);
            _buttons.Add(keyHint);

            if (_flow.Step == AddEventFlowModel.StepChooseType)
            {
                _buttons.Add(MakeButton(LiveOpsHubStrings.CalendarAddCancelButton, Close, false));
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
            if (isPrimary) button.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            return button;
        }

        internal void Advance()
        {
            if (!_flow.CanAdvance()) return;
            if (_flow.Step == AddEventFlowModel.StepChooseType)
            {
                _flow = _flow.WithType(_flow.EventType);
                Refresh();
                return;
            }
            if (_flow.Step == AddEventFlowModel.StepChooseTimes)
            {
                _flow = _flow.WithTimes(_flow.StartDateText, _flow.StartTimeText, _flow.DurationHours);
                Refresh();
            }
        }

        internal void Submit()
        {
            _submit(_flow);
            Close();
        }

        internal void Close()
        {
            if (editorWindow != null) editorWindow.Close();
        }

        /// <summary>Enter = bước tiếp; ở bước xem lại có chồng giờ thì Enter là "Quay lại sửa giờ", KHÔNG phải "Vẫn thêm" (7.0 (c)).</summary>
        internal void OnKeyDown(KeyDownEvent keyEvent)
        {
            if (keyEvent.keyCode == KeyCode.Escape)
            {
                keyEvent.StopPropagation();
                Close();
                return;
            }
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
