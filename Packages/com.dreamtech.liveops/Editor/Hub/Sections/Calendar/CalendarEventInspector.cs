using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Pane inspector đợt của màn Lịch [SD1 §3.1, §3.10]: pane-title (swatch · id mono · tag giai đoạn) và thân field. View MỎNG —
    /// mọi quyết định (trạng thái nào, khoá mép đầu không, nhãn nút xoá, câu nguồn, chữ dẫn config key) đọc từ
    /// <see cref="CalendarInspectorModel"/>; mọi lệnh sửa đi qua <see cref="CalendarTimelinePresenter"/> để mức xác nhận, toast và
    /// bước Undo giống hệt đường kéo trên trục.
    /// </summary>
    internal sealed class CalendarEventInspector
    {
        private readonly LiveOpsHubServices _services;
        private readonly CalendarTimelinePresenter _presenter;
        private readonly VisualElement _titleHost;
        private readonly VisualElement _bodyHost;

        // PD-14: nút đóng dùng icon "clear" — winbtn_win_close NULL ở 6000.6.
        private const string CloseIconName = "clear";
        private const int CloseIconSize = 12;

        private CalendarInspectorModel _model;

        public CalendarEventInspector(LiveOpsHubServices services, CalendarTimelinePresenter presenter, VisualElement titleHost,
            VisualElement bodyHost)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _titleHost = titleHost ?? throw new ArgumentNullException(nameof(titleHost));
            _bodyHost = bodyHost ?? throw new ArgumentNullException(nameof(bodyHost));
            // (G-OPT-TIMELINE, Hình 12 khung 9) Inspector tự nghe tập chọn nhiều thay vì chờ màn gọi: đường của màn là
            // Refresh(mot khoá) — nó KHÔNG diễn tả được "hai đợt", và ép nó diễn tả được thì mọi nơi gọi Refresh phải đổi theo.
            _presenter.SelectionSetChanged += OnSelectionSetChanged;
        }

        /// <summary>
        /// Nhả đăng ký <see cref="CalendarTimelinePresenter.SelectionSetChanged"/>. Màn gọi trước khi dựng inspector mới:
        /// presenter sống lâu hơn inspector (một thể hiện mỗi lần <c>CreateView</c>), nên không nhả là mỗi lần vào màn lại
        /// thêm một người nghe vẽ lên <c>_titleHost</c>/<c>_bodyHost</c> của cây element đã bỏ.
        /// </summary>
        internal void Detach()
        {
            _presenter.SelectionSetChanged -= OnSelectionSetChanged;
        }

        /// <summary>Bấm "Thêm đợt" ở trạng thái (a) — section mở popover.</summary>
        public event Action AddEventRequested;

        /// <summary>Bấm "Mở luật" ở trạng thái (b) hoặc link "Sửa ở Loại event".</summary>
        public event Action<LiveOpsHubNavigation> NavigationRequested;

        /// <summary>Bấm nút đóng của drawer (chỉ có nghĩa ở <c>--medium</c>) — màn giấu inspector [SD1 §3.9].</summary>
        public event Action CloseDrawerRequested;

        /// <summary>
        /// (mục 12 I-4) Nút đề xuất trong "Vấn đề (n)" mở popover Đề xuất của màn Kiểm lịch với lựa chọn vừa bấm CHỌN SẴN. Section
        /// đặt móc này vì popover cần <c>LayoutLoader</c>, kiểm nhanh trên nháp và chỗ neo — ba thứ của màn, không của inspector.
        /// </summary>
        internal Action<LiveEventCalendarFinding, int> ProposalRequested { get; set; }

        public CalendarInspectorModel Model => _model;

        /// <summary>
        /// Đưa focus về field đầu tiên của inspector — dùng cho mục menu "Sửa trong inspector" và cho nhánh "Sửa nhanh…" của một
        /// phát hiện KHÔNG có cách sửa tự động. Không có field nào (trạng thái (a)) thì không làm gì, không ném.
        /// </summary>
        public void FocusFirstField()
        {
            TextField field = _bodyHost.Q<TextField>();
            if (field != null)
            {
                field.Focus();
                return;
            }
            _bodyHost.Q<IntegerField>()?.Focus();
        }

        /// <summary>Tập chọn từ hai đợt trở lên → trạng thái (c); tập nhỏ hơn đi đường cũ (màn gọi <see cref="Refresh"/>).</summary>
        private void OnSelectionSetChanged(IReadOnlyList<string> barKeys)
        {
            if (barKeys == null || barKeys.Count < 2) return;
            RefreshMultiple(barKeys);
        }

        /// <summary>
        /// (c) chọn nhiều đợt [SD1 §3.10]: titlebar "2 đợt · lava-quest", ô "Dời cả hai (giờ)" + nút Áp, nút "Xoá 2 đợt…".
        /// Không dựng <see cref="CalendarInspectorModel"/> — model đó tả MỘT đợt, và ở đây không có đợt nào là "đợt đang xem".
        /// </summary>
        internal void RefreshMultiple(IReadOnlyList<string> barKeys)
        {
            _model = null;
            _titleHost.Clear();
            _bodyHost.Clear();

            Label title = new Label(MultiSelectTitleText(barKeys));
            title.AddToClassList(LiveOpsHubClassNames.CalendarInspectorTitleId);
            _titleHost.Add(title);
            _titleHost.Add(BuildCloseDrawerButton());

            IntegerField shiftHours = new IntegerField(LiveOpsHubStrings.TimelineMultiSelectShiftFieldLabel) { value = 0 };
            VisualElement shiftRow = new VisualElement();
            shiftRow.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            shiftRow.Add(shiftHours);
            Button apply = new Button(() => _presenter.ShiftSelectedEvents(shiftHours.value))
            {
                text = LiveOpsHubStrings.TimelineMultiSelectApplyButton,
            };
            apply.AddToClassList(LiveOpsHubClassNames.Button);
            shiftRow.Add(apply);
            _bodyHost.Add(shiftRow);

            Label help = new Label(LiveOpsHubStrings.TimelineMultiSelectHelpText);
            help.AddToClassList(LiveOpsHubClassNames.Note);
            _bodyHost.Add(help);

            Button delete = new Button(() => _presenter.DeleteSelectedEvents())
            {
                text = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectDeleteButtonFormat), barKeys.Count),
            };
            delete.AddToClassList(LiveOpsHubClassNames.Button);
            delete.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            _bodyHost.Add(delete);
        }

        /// <summary>"2 đợt · lava-quest" khi cả tập cùng một loại; "3 đợt · 2 loại" khi lẫn loại — titlebar không bịa một tên.</summary>
        private string MultiSelectTitleText(IReadOnlyList<string> barKeys)
        {
            var eventTypes = new List<string>();
            for (int index = 0; index < barKeys.Count; index++)
            {
                LiveOpsTimelineBarModel bar = _presenter.FindBar(barKeys[index]);
                if (bar == null || bar.EventType.Length == 0) continue;
                if (!eventTypes.Contains(bar.EventType)) eventTypes.Add(bar.EventType);
            }
            string laneText = eventTypes.Count == 1
                ? eventTypes[0]
                : LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectMixedTypesFormat), eventTypes.Count);
            return LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectTitleFormat), barKeys.Count, laneText);
        }

        public void Refresh(string selectedBarKey)
        {
            _model = CalendarInspectorModel.Build(_services.Session, selectedBarKey, _services.Clock.UtcNow, _services.Format,
                _presenter.FindBar(selectedBarKey));
            _titleHost.Clear();
            _bodyHost.Clear();
            BuildTitle();
            switch (_model.State)
            {
                case CalendarInspectorModel.StateNothingSelected:
                    BuildNothingSelected();
                    return;
                case CalendarInspectorModel.StateRecurringEvent:
                    BuildRecurring();
                    return;
                default:
                    BuildFixed();
                    return;
            }
        }

        private void BuildTitle()
        {
            if (_model.State == CalendarInspectorModel.StateNothingSelected) return;
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            LiveOpsHubStyle.SetEventColor(swatch, _model.EventTypeDefinition == null ? 0 : _model.EventTypeDefinition.ColorSlot);
            _titleHost.Add(swatch);

            Label id = new Label(_model.EventId);
            id.AddToClassList(LiveOpsHubClassNames.CalendarInspectorTitleId);
            id.AddToClassList(LiveOpsHubClassNames.Mono);
            id.tooltip = _model.ChangedTooltip;
            _titleHost.Add(id);

            if (_model.PhaseTagText.Length > 0)
            {
                Label phase = new Label(_model.PhaseTagText);
                phase.AddToClassList(LiveOpsHubClassNames.Tag);
                LiveOpsHubStyle.SetPhase(phase, _model.Phase, false);
                _titleHost.Add(phase);
            }
            _titleHost.Add(BuildCloseDrawerButton());
        }

        /// <summary>
        /// Nút đóng của drawer [SD1 §3.9]. Luôn có trong cây nhưng USS chỉ hiện nó ở <c>--medium</c>: ở cửa sổ rộng inspector là
        /// một cột cố định, không đóng được, nên một nút đóng ở đó vừa vô nghĩa vừa làm mọi ảnh Hình 1 khác đi.
        /// </summary>
        private VisualElement BuildCloseDrawerButton()
        {
            Button close = new Button(() => CloseDrawerRequested?.Invoke())
            {
                tooltip = LiveOpsHubStrings.CalendarDepthDrawerCloseTooltip,
                name = LiveOpsHubPaths.CalendarDepthElementNames.InspectorDrawerClose,
            };
            close.AddToClassList(LiveOpsHubClassNames.CalendarDepthDrawerClose);
            close.Add(LiveOpsHubIcons.CreateImage(CloseIconName, CloseIconSize));
            return close;
        }

        private void BuildNothingSelected()
        {
            VisualElement empty = new VisualElement();
            empty.AddToClassList(LiveOpsHubClassNames.Empty);
            empty.Add(new Label(LiveOpsHubStrings.CalendarInspectorEmptyText));
            Label summary = new Label(_model.SummaryText);
            summary.AddToClassList(LiveOpsHubClassNames.Caption);
            empty.Add(summary);
            Button add = new Button(() => AddEventRequested?.Invoke()) { text = LiveOpsHubStrings.CalendarAddEventButton };
            add.AddToClassList(LiveOpsHubClassNames.Button);
            add.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            empty.Add(add);
            _bodyHost.Add(empty);
        }

        /// <summary>
        /// (b) đợt sinh từ luật [SD1 §3.10]: vẽ ĐỦ field như đợt cố định rồi <c>SetEnabled(false)</c>. Bỏ hẳn field thì pane (b)
        /// trông như một màn khác và người dùng không đối chiếu được giờ của lần lặp; hiện field xám nói đúng "xem được, sửa ở
        /// Luật lặp".
        /// </summary>
        private void BuildRecurring()
        {
            VisualElement fields = new VisualElement();
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldIdLabel, _model.EventId, true));
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldTypeLabel, _model.EventTypeId, true));
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldStartLabel, OccurrenceText(_model.OccurrenceStartUtc), false));
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldEndLabel, OccurrenceText(_model.OccurrenceEndUtc), false));
            fields.Add(BuildReadOnlyDurationRow());
            fields.SetEnabled(false);
            _bodyHost.Add(fields);

            Label note = new Label(_model.RecurringNoteText);
            note.AddToClassList(LiveOpsHubClassNames.Note);
            _bodyHost.Add(note);
            Button openRule = new Button(() => NavigationRequested?.Invoke(
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.RecurringRules).WithEventType(_model.RuleEventType, true)))
            {
                text = LiveOpsHubStrings.CalendarOpenRuleButton,
            };
            openRule.AddToClassList(LiveOpsHubClassNames.Button);
            _bodyHost.Add(openRule);
            _bodyHost.Add(SourceRow());
        }

        private TextField ReadOnlyTextField(string label, string value, bool isMono)
        {
            TextField field = new TextField(label) { value = value, isReadOnly = true };
            if (isMono) field.AddToClassList(LiveOpsHubClassNames.Mono);
            return field;
        }

        private string OccurrenceText(DateTime? utc)
        {
            return utc == null ? string.Empty : _services.Format.ShortDateTimeUtc(utc.Value);
        }

        private VisualElement BuildReadOnlyDurationRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            row.Add(new IntegerField(LiveOpsHubStrings.CalendarFieldDurationLabel) { value = _model.DurationHours, isReadOnly = true });
            row.Add(new Label(LiveOpsHubStrings.CalendarDurationUnitLabel));
            row.Add(AddEventPopover.BuildDurationNote(_model.DurationHours, _services.Format));
            return row;
        }

        private void BuildFixed()
        {
            FixedLiveEventEntry entry = _model.Entry;
            _bodyHost.Add(BuildIdField(entry));
            _bodyHost.Add(BuildTypeField(entry));
            _bodyHost.Add(BuildTimeField(entry, false));
            _bodyHost.Add(BuildTimeField(entry, true));
            _bodyHost.Add(BuildDurationField(entry));
            _bodyHost.Add(BuildConfigKeyField(entry));
            _bodyHost.Add(BuildRequiresOptInRow());
            _bodyHost.Add(SourceRow());

            VisualElement separator = new VisualElement();
            separator.AddToClassList(LiveOpsHubClassNames.CalendarInspectorSeparator);
            _bodyHost.Add(separator);
            _bodyHost.Add(BuildIssuesFoldout());
            _bodyHost.Add(BuildDeleteButton(entry));
        }

        private VisualElement BuildIdField(FixedLiveEventEntry entry)
        {
            TextField field = new TextField(LiveOpsHubStrings.CalendarFieldIdLabel) { value = entry.EventId, isDelayed = true };
            field.AddToClassList(LiveOpsHubClassNames.Mono);
            field.RegisterValueChangedCallback(change =>
            {
                string newId = (change.newValue ?? string.Empty).Trim();
                if (newId.Length == 0 || string.Equals(newId, entry.EventId, StringComparison.Ordinal)) return;
                string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRenameToastFormat,
                    entry.EventId, newId);
                _presenter.ApplyEdit(new ReplaceFixedEventEdit(entry.WithEventId(newId)),
                    LiveOpsEditOperation.RenameOrRetypeFixedEvent, entry.EntryKey, message, string.Empty,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRenameUndoStepFormat, entry.EventId));
            });
            return field;
        }

        /// <summary>
        /// Ô "Loại" (7.3: "swatch + id; chỉ loại cố định"). Danh sách lấy từ CHÍNH hàm mà popover Thêm đợt dùng — loại có luật lặp
        /// không tạo đợt cố định được nên không được có mặt ở đây; swatch đứng trước ô để màu của loại đang chọn đọc được ngay.
        /// </summary>
        private VisualElement BuildTypeField(FixedLiveEventEntry entry)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);

            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            LiveOpsHubStyle.SetEventColor(swatch, _model.EventTypeDefinition == null ? 0 : _model.EventTypeDefinition.ColorSlot);
            row.Add(swatch);

            List<string> choices = new List<string>();
            IReadOnlyList<AddEventTypeChoice> fixedChoices = AddEventFlowModel.TypeChoicesOf(
                _services.Session.Document ?? LiveEventCalendarDocument.Empty, string.Empty);
            for (int index = 0; index < fixedChoices.Count; index++)
            {
                if (fixedChoices[index].IsEnabled) choices.Add(fixedChoices[index].TypeId);
            }
            // Loại của đợt đang xem có thể là loại lạ (JSON đã dán) hoặc loại có luật lặp: vẫn phải hiện, kẻo ô rơi về loại khác.
            if (IndexOfType(choices, entry.EventType) < 0 && entry.EventType.Length > 0) choices.Insert(0, entry.EventType);
            DropdownField field = new DropdownField(LiveOpsHubStrings.CalendarFieldTypeLabel, choices,
                Math.Max(0, IndexOfType(choices, entry.EventType)));
            field.RegisterValueChangedCallback(change =>
            {
                string newType = change.newValue ?? string.Empty;
                if (newType.Length == 0 || string.Equals(newType, entry.EventType, StringComparison.Ordinal)) return;
                string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRetypeToastFormat,
                    entry.EventId, newType);
                _presenter.ApplyEdit(new ReplaceFixedEventEdit(entry.WithEventType(newType)),
                    LiveOpsEditOperation.RenameOrRetypeFixedEvent, entry.EntryKey, message, string.Empty,
                    string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRetypeUndoStepFormat, entry.EventId));
            });
            row.Add(field);
            return row;
        }

        /// <summary>
        /// Ô giờ chỉ nhận UTC [SD1 §3.13]. Chuỗi không đọc được VẪN ghi vào asset nguyên văn: tài liệu phải chứa được đợt hỏng để
        /// hub và bộ kiểm cùng thấy và báo lỗi, nên field bắt <c>RawTextCommitted</c> chứ không chỉ giá trị đã parse.
        /// </summary>
        private static int IndexOfType(List<string> choices, string typeId)
        {
            for (int index = 0; index < choices.Count; index++)
            {
                if (string.Equals(choices[index], typeId, StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private VisualElement BuildTimeField(FixedLiveEventEntry entry, bool isEnd)
        {
            string label = isEnd ? LiveOpsHubStrings.CalendarFieldEndLabel : LiveOpsHubStrings.CalendarFieldStartLabel;
            LiveOpsUtcDateTimeField field = new LiveOpsUtcDateTimeField(label);
            field.SetDeviceOffset(_services.TimeZone.DeviceOffsetAt(_services.Clock.UtcNow));
            string rawText = isEnd ? entry.EndUtcText : entry.StartUtcText;
            if (LiveEventUtcText.TryParse(rawText, out DateTime utc))
            {
                field.SetValueWithoutNotify(utc);
            }
            else
            {
                LiveOpsUtcDateTimeField.SplitRawText(rawText, out string rawDateText, out string rawTimeText);
                field.SetRawTextWithoutNotify(rawDateText, rawTimeText);
            }

            if (!isEnd && _model.IsStartLocked)
            {
                field.SetEnabled(false);
                field.tooltip = _model.StartLockReason;
                return field;
            }
            // Ô giờ báo hai đường và inspector phải nghe CẢ HAI: chuỗi đọc được đi bằng ChangeEvent<DateTime>, chuỗi hỏng đi bằng
            // RawTextCommitted. Bản trước chỉ nghe đường thứ hai, nên gõ đúng dạng rồi Enter/Tab/bấm ra ngoài KHÔNG ghi gì vào
            // asset — ô hiện giá trị mới nên người dùng tưởng đã ăn, tới lần dựng lại mới thấy mất (UJ-03).
            field.RegisterValueChangedCallback(change => CommitTime(entry, isEnd, LiveEventUtcText.Format(change.newValue)));
            field.RawTextCommitted += (dateText, timeText) => CommitTime(entry, isEnd, JoinRawText(dateText, timeText));
            if (_model.State == CalendarInspectorModel.StateUnreadableTimes && _model.IsUnreadableEnd == isEnd)
            {
                field.SetErrorText(_model.UnreadableFieldErrorText);
            }
            return field;
        }

        /// <summary>
        /// Ghi một mép giờ. <paramref name="timeUtcText"/> đã là chuỗi cuối cùng sẽ nằm trong asset — dạng chuẩn khi đọc được,
        /// nguyên văn người dùng gõ khi không (tài liệu phải chứa được đợt hỏng để bộ kiểm báo đúng chuỗi).
        /// </summary>
        private void CommitTime(FixedLiveEventEntry entry, bool isEnd, string timeUtcText)
        {
            FixedLiveEventEntry next = isEnd
                ? entry.WithTimes(entry.StartUtcText, timeUtcText)
                : entry.WithTimes(timeUtcText, entry.EndUtcText);
            if (string.Equals(next.StartUtcText, entry.StartUtcText, StringComparison.Ordinal)
                && string.Equals(next.EndUtcText, entry.EndUtcText, StringComparison.Ordinal))
            {
                return;
            }
            string message = _presenter.DragToastMessage(entry, next);
            _presenter.ApplyEdit(new ReplaceFixedEventEdit(next), LiveOpsEditOperation.ChangeFixedEventTimes, entry.EntryKey,
                message, string.Empty, _presenter.DragUndoStepName(entry, next));
        }

        /// <summary>
        /// Ghép nửa ngày và nửa giờ của chuỗi KHÔNG đọc được. Dấu cách là dấu ngăn cố ý (chữ người dùng gõ chưa phải ISO nên ghép
        /// bằng "T" là bịa thêm dạng chuẩn cho một chuỗi hỏng); <see cref="LiveOpsUtcDateTimeField.SplitRawText"/> tách lại được
        /// đúng hai nửa đó.
        /// </summary>
        private static string JoinRawText(string dateText, string timeText)
        {
            return dateText + (timeText.Length > 0 ? " " + timeText : string.Empty);
        }

        /// <summary>Đổi "Dài" giữ nguyên giờ bắt đầu (7.3) — người dùng nghĩ theo "đợt chạy mấy giờ", không theo "kết thúc lúc nào".</summary>
        private VisualElement BuildDurationField(FixedLiveEventEntry entry)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            IntegerField field = new IntegerField(LiveOpsHubStrings.CalendarFieldDurationLabel)
            {
                value = _model.DurationHours,
                isDelayed = true,
            };
            field.RegisterValueChangedCallback(change =>
            {
                if (change.newValue <= 0 || !entry.TryGetStartUtc(out DateTime startUtc)) return;
                FixedLiveEventEntry next = entry.WithTimes(entry.StartUtcText,
                    LiveEventUtcText.Format(startUtc.AddHours(change.newValue)));
                string message = _presenter.DragToastMessage(entry, next);
                _presenter.ApplyEdit(new ReplaceFixedEventEdit(next), LiveOpsEditOperation.ChangeFixedEventTimes, entry.EntryKey,
                    message, string.Empty, _presenter.DragUndoStepName(entry, next));
            });
            row.Add(field);
            row.Add(new Label(LiveOpsHubStrings.CalendarDurationUnitLabel));
            row.Add(AddEventPopover.BuildDurationNote(_model.DurationHours, _services.Format));
            return row;
        }

        private VisualElement BuildConfigKeyField(FixedLiveEventEntry entry)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            TextField field = new TextField(LiveOpsHubStrings.CalendarFieldConfigKeyLabel)
            {
                value = entry.ConfigKey,
                isDelayed = true,
            };
            field.AddToClassList(LiveOpsHubClassNames.Mono);
            LiveOpsPlaceholder.Attach(field, _model.ConfigKeyPlaceholder);
            field.RegisterValueChangedCallback(change => CommitConfigKey(entry, change.newValue ?? string.Empty));
            row.Add(field);

            Button reset = new Button(() => CommitConfigKey(entry, string.Empty))
            {
                text = LiveOpsHubStrings.CalendarConfigKeyResetButton,
                tooltip = _model.ConfigKeyResetTooltip,
            };
            reset.AddToClassList(LiveOpsHubClassNames.Button);
            reset.SetEnabled(_model.HasOwnConfigKey);
            row.Add(reset);
            return row;
        }

        private void CommitConfigKey(FixedLiveEventEntry entry, string configKey)
        {
            if (string.Equals(configKey, entry.ConfigKey, StringComparison.Ordinal)) return;
            string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarConfigKeyToastFormat, entry.EventId);
            _presenter.ApplyEdit(new ReplaceFixedEventEdit(entry.WithConfigKey(configKey)), LiveOpsEditOperation.EditEventTypeFields,
                entry.EntryKey, message, string.Empty,
                string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarConfigKeyUndoStepFormat, entry.EventId));
        }

        /// <summary>"Phải bấm tham gia" thuộc loại event nên ở đây chỉ đọc; link dẫn đúng chỗ sửa được (một nguồn sự thật).</summary>
        private VisualElement BuildRequiresOptInRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            row.Add(new Label(LiveOpsHubStrings.CalendarFieldRequiresOptInLabel));
            row.Add(new Label(_model.RequiresOptInText));
            Button link = new Button(() => NavigationRequested?.Invoke(
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.EventTypes).WithEventType(_model.EventTypeId, true)))
            {
                text = LiveOpsHubStrings.CalendarEditTypeLink,
            };
            link.AddToClassList(LiveOpsHubClassNames.Button);
            row.Add(link);
            return row;
        }

        private VisualElement SourceRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            row.Add(new Label(LiveOpsHubStrings.CalendarFieldSourceLabel));
            row.Add(new Label(_model.SourceText));
            return row;
        }

        private VisualElement BuildIssuesFoldout()
        {
            Foldout foldout = new Foldout
            {
                text = _model.IssuesFoldoutText,
                value = _model.Findings.Count > 0,
                viewDataKey = LiveOpsHubClassNames.CalendarInspectorIssues,
            };
            foldout.AddToClassList(LiveOpsHubClassNames.CalendarInspectorIssues);
            for (int index = 0; index < _model.Findings.Count; index++) foldout.Add(BuildFindingCard(_model.Findings[index]));
            if (_model.State == CalendarInspectorModel.StateUnreadableTimes && _model.UnreadableFixButtonText.Length > 0)
            {
                foldout.Add(BuildUnreadableFixButton());
            }
            return foldout;
        }

        private VisualElement BuildFindingCard(LiveEventCalendarFinding finding)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList(LiveOpsHubClassNames.FindingRow);
            LiveOpsHubStyle.SetSeverityStripe(card, LiveOpsHubFindingRouting.StateOf(finding.Consequence));

            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            Label headline = new Label(LiveOpsFindingText.Headline(finding, _services.Format, document.LatestStamp));
            LiveOpsHubStyle.SetStateText(headline, LiveOpsHubFindingRouting.StateOf(finding.Consequence));
            card.Add(headline);

            Label meta = new Label(LiveOpsFindingText.Meta(finding, _services.Format, _services.Clock.UtcNow));
            meta.AddToClassList(LiveOpsHubClassNames.Caption);
            card.Add(meta);

            IReadOnlyList<LiveEventCalendarRepair> repairs = finding.Repairs;
            for (int index = 0; index < repairs.Count; index++) card.Add(BuildRepairButton(finding, repairs[index], index));
            return card;
        }

        /// <summary>
        /// (mục 12 I-4) Nút đề xuất KHÔNG áp gì: nó mở <c>ProposalPopover</c> của màn Kiểm lịch với lựa chọn vừa bấm chọn sẵn.
        /// Áp thẳng từ inspector bỏ qua kiểm nhanh và câu hậu quả của popover — hai thứ duy nhất nói trước điều sắp xảy ra.
        /// </summary>
        private VisualElement BuildRepairButton(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair, int repairIndex)
        {
            Button button = new Button(() => ProposalRequested?.Invoke(finding, repairIndex))
            {
                text = LiveOpsFindingText.RepairOptionText(finding, repair, _services.Format),
                tooltip = LiveOpsFindingText.ManualFixSentence(finding, _services.Format),
            };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            return button;
        }

        private VisualElement BuildUnreadableFixButton()
        {
            FixedLiveEventEntry entry = _model.Entry;
            Button button = new Button(() =>
            {
                FixedLiveEventEntry next = _model.IsUnreadableEnd
                    ? entry.WithTimes(entry.StartUtcText, _model.UnreadableFixValueText)
                    : entry.WithTimes(_model.UnreadableFixValueText, entry.EndUtcText);
                string message = _presenter.DragToastMessage(entry, next);
                _presenter.ApplyEdit(new ReplaceFixedEventEdit(next), LiveOpsEditOperation.ChangeFixedEventTimes, entry.EntryKey,
                    message, string.Empty, _presenter.DragUndoStepName(entry, next));
            })
            {
                text = _model.UnreadableFixButtonText,
            };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            return button;
        }

        private VisualElement BuildDeleteButton(FixedLiveEventEntry entry)
        {
            Button button = new Button(() => _presenter.RequestDelete(entry.EntryKey))
            {
                text = _model.DeleteButtonText,
                tooltip = _model.DeleteButtonTooltip,
            };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            button.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            return button;
        }
    }
}
