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

        private CalendarInspectorModel _model;

        public CalendarEventInspector(LiveOpsHubServices services, CalendarTimelinePresenter presenter, VisualElement titleHost,
            VisualElement bodyHost)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _titleHost = titleHost ?? throw new ArgumentNullException(nameof(titleHost));
            _bodyHost = bodyHost ?? throw new ArgumentNullException(nameof(bodyHost));
        }

        /// <summary>Bấm "Thêm đợt" ở trạng thái (a) — section mở popover.</summary>
        public event Action AddEventRequested;

        /// <summary>Bấm "Mở luật" ở trạng thái (b) hoặc link "Sửa ở Loại event".</summary>
        public event Action<LiveOpsHubNavigation> NavigationRequested;

        public CalendarInspectorModel Model => _model;

        public void Refresh(string selectedBarKey)
        {
            _model = CalendarInspectorModel.Build(_services.Session, selectedBarKey, _services.Clock.UtcNow, _services.Format);
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

            if (_model.PhaseTagText.Length == 0) return;
            Label phase = new Label(_model.PhaseTagText);
            phase.AddToClassList(LiveOpsHubClassNames.Tag);
            LiveOpsHubStyle.SetPhase(phase, _model.Phase, false);
            _titleHost.Add(phase);
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

        private void BuildRecurring()
        {
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
                    LiveOpsEditOperation.RenameOrRetypeFixedEvent, entry.EntryKey, message, string.Empty);
            });
            return field;
        }

        private VisualElement BuildTypeField(FixedLiveEventEntry entry)
        {
            List<string> choices = new List<string>();
            IReadOnlyList<LiveEventTypeDefinition> types = (_services.Session.Document ?? LiveEventCalendarDocument.Empty).EventTypes;
            for (int index = 0; index < types.Count; index++) choices.Add(types[index].TypeId);
            DropdownField field = new DropdownField(LiveOpsHubStrings.CalendarFieldTypeLabel, choices,
                Math.Max(0, IndexOfType(choices, entry.EventType)));
            field.RegisterValueChangedCallback(change =>
            {
                string newType = change.newValue ?? string.Empty;
                if (newType.Length == 0 || string.Equals(newType, entry.EventType, StringComparison.Ordinal)) return;
                string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRetypeToastFormat,
                    entry.EventId, newType);
                _presenter.ApplyEdit(new ReplaceFixedEventEdit(entry.WithEventType(newType)),
                    LiveOpsEditOperation.RenameOrRetypeFixedEvent, entry.EntryKey, message, string.Empty);
            });
            return field;
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
            if (LiveEventUtcText.TryParse(rawText, out DateTime utc)) field.SetValueWithoutNotify(utc);
            else field.SetRawTextWithoutNotify(DatePartOf(rawText), TimePartOf(rawText));

            if (!isEnd && _model.IsStartLocked)
            {
                field.SetEnabled(false);
                field.tooltip = _model.StartLockReason;
                return field;
            }
            field.RawTextCommitted += (dateText, timeText) => CommitTime(entry, isEnd, dateText, timeText);
            if (_model.State == CalendarInspectorModel.StateUnreadableTimes && _model.IsUnreadableEnd == isEnd)
            {
                field.SetErrorText(_model.UnreadableFieldErrorText);
            }
            return field;
        }

        private void CommitTime(FixedLiveEventEntry entry, bool isEnd, string dateText, string timeText)
        {
            string canonical = LiveOpsUtcDateTimeField.TryParseParts(dateText, timeText, out DateTime utc)
                ? LiveEventUtcText.Format(utc)
                : dateText + (timeText.Length > 0 ? " " + timeText : string.Empty);
            FixedLiveEventEntry next = isEnd
                ? entry.WithTimes(entry.StartUtcText, canonical)
                : entry.WithTimes(canonical, entry.EndUtcText);
            string message = _presenter.DragToastMessage(entry, next);
            _presenter.ApplyEdit(new ReplaceFixedEventEdit(next), LiveOpsEditOperation.ChangeFixedEventTimes, entry.EntryKey,
                message, string.Empty);
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
                    message, string.Empty);
            });
            row.Add(field);
            row.Add(new Label(LiveOpsHubStrings.CalendarDurationUnitLabel));
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
                entry.EntryKey, message, string.Empty);
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
            for (int index = 0; index < repairs.Count; index++) card.Add(BuildRepairButton(finding, repairs[index]));
            return card;
        }

        // INTERIM(G-CALENDAR-DEPTH): nút đề xuất ở đây sửa THẲNG field thay vì mở ProposalPopover của Kiểm lịch (mục 12 I-4).
        // Vẫn đủ an toàn: một Undo group + toast, và không áp gì khi người dùng chưa bấm. W5 đổi sang mở popover chọn sẵn lựa chọn này.
        private VisualElement BuildRepairButton(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            Button button = new Button(() =>
            {
                string message = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarRepairToastFormat,
                    finding.TargetId);
                _presenter.ApplyEdit(repair.Edit, LiveOpsEditOperation.ApplyProposal, finding.TargetEntryKey, message, string.Empty);
            })
            {
                text = LiveOpsFindingText.RepairOptionText(finding, repair, _services.Format),
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
                    message, string.Empty);
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

        private static string DatePartOf(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;
            int timeIndex = rawText.IndexOf('T');
            return timeIndex < 0 ? rawText : rawText.Substring(0, timeIndex);
        }

        private static string TimePartOf(string rawText)
        {
            if (string.IsNullOrEmpty(rawText)) return string.Empty;
            int timeIndex = rawText.IndexOf('T');
            if (timeIndex < 0 || timeIndex + 1 >= rawText.Length) return string.Empty;
            string time = rawText.Substring(timeIndex + 1).TrimEnd('Z');
            return time.Length > 5 ? time.Substring(0, 5) : time;
        }
    }
}
