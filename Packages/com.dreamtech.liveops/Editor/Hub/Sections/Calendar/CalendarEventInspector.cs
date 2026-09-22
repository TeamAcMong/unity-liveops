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
            // Thân inspector KHÔNG bao giờ cuộn ngang [SD1 §3.1]: thanh cuộn ngang ở đây không phải tính năng mà là dấu hiệu
            // nội dung đã tràn — người dùng không kéo nó, chỉ thấy chữ cụt (C3). Ẩn thanh để mọi chỗ tràn lộ ra ở cổng bố cục.
            ScrollView scrollableBody = bodyHost as ScrollView;
            if (scrollableBody != null) scrollableBody.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
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
            shiftHours.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            shiftHours.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldNumber);
            // (W9-25 chỗ 1) Nhãn của RIÊNG ô này được phép xuống dòng. "Shift both (hours)" cần 92px chữ trong cột nhãn 96px,
            // tức 93px vùng nội dung — dư đúng 1px, nên một đổi metric font là cắt mất "(hours)". Cột nhãn 96px KHÔNG nới
            // được: đo ở 1280x760, hàng ô giờ UTC đã dùng 161px trong 168px mà cột field còn lại, nới nhãn thêm 8px là đẩy
            // nhãn "UTC" ra ngoài pane. Cho nhãn wrap thì chữ không bao giờ bị cắt theo bề rộng nữa mà hàng vẫn y hệt hôm
            // nay (92 < 93 nên tiếng Anh vẫn một dòng) — và pane chọn nhiều chỉ có MỘT field nên không có cột nào để lệch.
            shiftHours.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            VisualElement shiftRow = BuildFieldRow();
            shiftRow.Add(shiftHours);
            Button apply = new Button(() => _presenter.ShiftSelectedEvents(shiftHours.value))
            {
                text = LiveOpsHubStrings.TimelineMultiSelectApplyButton,
            };
            apply.AddToClassList(LiveOpsHubClassNames.Button);
            shiftRow.Add(apply);
            _bodyHost.Add(shiftRow);

            _bodyHost.Add(BuildNote(LiveOpsHubStrings.TimelineMultiSelectHelpText));

            Button delete = new Button(() => _presenter.DeleteSelectedEvents())
            {
                text = LiveOpsHubStringCatalog.Format(nameof(LiveOpsHubStrings.TimelineMultiSelectDeleteButtonFormat), barKeys.Count),
            };
            delete.AddToClassList(LiveOpsHubClassNames.Button);
            delete.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            delete.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            // (J3-01) _bodyHost là hộp xếp DỌC — nút phá huỷ không được lấy trọn bề ngang pane chọn nhiều.
            delete.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
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
            Label emptyText = new Label(LiveOpsHubStrings.CalendarInspectorEmptyText);
            emptyText.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            empty.Add(emptyText);
            Label summary = new Label(_model.SummaryText);
            summary.AddToClassList(LiveOpsHubClassNames.Caption);
            summary.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            empty.Add(summary);
            Button add = new Button(() => AddEventRequested?.Invoke()) { text = LiveOpsHubStrings.CalendarAddEventButton };
            add.AddToClassList(LiveOpsHubClassNames.Button);
            add.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            // (J3-01) Khối rỗng xếp DỌC và canh TRÁI (max-width 520px) — nút phải rộng bằng chữ của nó, không bằng khối
            // (đo được 233px ở 1280x760 trước bản vá).
            add.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
            empty.Add(add);
            _bodyHost.Add(empty);
        }

        /// <summary>
        /// (b) đợt sinh từ luật [SD1 §3.10]: vẽ ĐỦ field như đợt cố định. Bỏ hẳn field thì pane (b) trông như một màn khác và
        /// người dùng không đối chiếu được giờ của lần lặp.
        /// <para>
        /// (W9-21) Pane này KHÔNG còn <c>SetEnabled(false)</c>. Vì sao: <c>:disabled</c> của Unity nhân opacity vào MÀU ĐÃ HỢP
        /// THÀNH, nên 12 đoạn chữ của pane đo được 2,28–3,54:1 trên nền cửa sổ — dưới xa 4,5:1 mà user chốt 19/9, và không token màu nào
        /// cứu được vì opacity nhân SAU token. WCAG 2.1 §1.4.3 miễn tương phản cho "thành phần giao diện KHÔNG hoạt động", mà
        /// pane này không phải thứ không hoạt động: nó là chỗ DUY NHẤT đọc được giờ của lần lặp đang chọn. Không sửa được vẫn
        /// giữ nguyên bằng thứ khoá thật — mọi field ở đây đều <c>isReadOnly</c>, tức không nhận chữ — còn dấu hiệu "không sửa
        /// ở đây" do viền trái + ghi chú "Sửa ở Luật lặp" ngay dưới mang, là CHỮ chứ không phải độ mờ (SPIKE-B SP-3).
        /// Đổi này KHÔNG đụng quy ước disabled của hub: nút bị chặn và hai mép giờ bị khoá vẫn mờ bằng cơ chế cũ, vì chúng đúng
        /// là thành phần không hoạt động.
        /// </para>
        /// </summary>
        private void BuildRecurring()
        {
            VisualElement fields = new VisualElement();
            fields.AddToClassList(LiveOpsHubClassNames.CalendarInspectorReadOnlyPane);
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldIdLabel, _model.EventId, true));
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldTypeLabel, _model.EventTypeId, true));
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldStartLabel, OccurrenceText(_model.OccurrenceStartUtc), false));
            fields.Add(ReadOnlyTextField(LiveOpsHubStrings.CalendarFieldEndLabel, OccurrenceText(_model.OccurrenceEndUtc), false));
            fields.Add(BuildReadOnlyDurationRow());
            _bodyHost.Add(fields);

            _bodyHost.Add(BuildNote(_model.RecurringNoteText));
            Button openRule = new Button(() => NavigationRequested?.Invoke(
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.RecurringRules).WithEventType(_model.RuleEventType, true)))
            {
                text = LiveOpsHubStrings.CalendarOpenRuleButton,
            };
            openRule.AddToClassList(LiveOpsHubClassNames.Button);
            openRule.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            _bodyHost.Add(openRule);
            _bodyHost.Add(SourceRow());
        }

        private TextField ReadOnlyTextField(string label, string value, bool isMono)
        {
            TextField field = new TextField(label) { value = value, isReadOnly = true };
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldText);
            if (isMono) field.AddToClassList(LiveOpsHubClassNames.Mono);
            return field;
        }

        /// <summary>Hàng field của inspector: hàng ngang CÓ WRAP — chỗ không đủ thì hậu tố/ghi chú/nút xuống dòng, không ra ngoài pane.</summary>
        private static VisualElement BuildFieldRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarFieldRow);
            row.AddToClassList(LiveOpsHubClassNames.CalendarInspectorRow);
            return row;
        }

        /// <summary>
        /// Khung ghi chú + Label wrap. Class <c>liveops-hub-note</c> một mình là một HÀNG NGANG có viền, không xuống dòng: gắn
        /// thẳng lên Label thì câu dài bị mép pane cắt mất nửa sau (C6, UJ-16).
        /// </summary>
        private static VisualElement BuildNote(string text)
        {
            VisualElement box = new VisualElement();
            box.AddToClassList(LiveOpsHubClassNames.Note);
            Label label = new Label(text);
            label.AddToClassList(LiveOpsHubClassNames.NoteText);
            box.Add(label);
            return box;
        }

        /// <summary>Hàng chỉ đọc: cột nhãn trùng cột nhãn của field, giá trị wrap (T10).</summary>
        private static VisualElement BuildReadOnlyRow(string label, string value)
        {
            VisualElement row = BuildFieldRow();
            Label name = new Label(label);
            name.AddToClassList(LiveOpsHubClassNames.CalendarInspectorLabel);
            row.Add(name);
            Label text = new Label(value);
            text.AddToClassList(LiveOpsHubClassNames.CalendarInspectorValue);
            row.Add(text);
            return row;
        }

        private string OccurrenceText(DateTime? utc)
        {
            return utc == null ? string.Empty : _services.Format.ShortDateTimeUtc(utc.Value);
        }

        private VisualElement BuildReadOnlyDurationRow()
        {
            VisualElement row = BuildFieldRow();
            IntegerField duration = new IntegerField(LiveOpsHubStrings.CalendarFieldDurationLabel)
            {
                value = _model.DurationHours,
                isReadOnly = true,
            };
            duration.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            duration.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldNumber);
            row.Add(duration);
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
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldText);
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
            VisualElement row = BuildFieldRow();

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
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldText);
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
        /// hub và bộ kiểm cùng thấy và báo lỗi, nên field bắt <c>TextCommitted</c> chứ không chỉ giá trị đã parse.
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
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
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

            // Mép nào bị khoá thì khoá ô ĐÓ và nói lý do ngay trên ô: đợt đang chạy khoá mép đầu (người chơi đã vào theo giờ cũ),
            // đợt đã khép khoá cả hai. Không có nhánh này thì ô vẫn nhận chữ + Enter rồi ApplyEdit bỏ lệnh không một lời nào —
            // người dùng đọc "23:00" trong ô mà asset còn giờ cũ (W8-UX2).
            bool isEdgeLocked = isEnd ? _model.IsEndLocked : _model.IsStartLocked;
            if (isEdgeLocked)
            {
                field.SetEnabled(false);
                field.tooltip = isEnd ? _model.EndLockReason : _model.StartLockReason;
                return field;
            }
            // Ô giờ chốt bằng ĐÚNG MỘT đường (TextCommitted), đọc được hay không cũng vậy: bản trước có hai đường (ChangeEvent cho
            // chuỗi đọc được, RawTextCommitted cho chuỗi hỏng) và nơi nghe chỉ đăng ký một đường là mất nửa số lần ghi mà không có
            // lỗi biên dịch nào — gõ đúng dạng rồi Enter/Tab/bấm ra ngoài KHÔNG ghi gì vào asset (UJ-03).
            field.TextCommitted += (dateText, timeText) =>
                CommitTime(entry, isEnd, LiveOpsUtcDateTimeField.ToAssetText(dateText, timeText));
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

        /// <summary>Đổi "Dài" giữ nguyên giờ bắt đầu (7.3) — người dùng nghĩ theo "đợt chạy mấy giờ", không theo "kết thúc lúc nào".</summary>
        private VisualElement BuildDurationField(FixedLiveEventEntry entry)
        {
            VisualElement row = BuildFieldRow();
            IntegerField field = new IntegerField(LiveOpsHubStrings.CalendarFieldDurationLabel)
            {
                value = _model.DurationHours,
                isDelayed = true,
            };
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldNumber);
            // "Dài" ghi vào MÉP CUỐI (giữ nguyên giờ bắt đầu), nên nó khoá theo đúng cái khoá của mép cuối — cùng lý do với ô giờ:
            // đợt đã khép thì lệnh ghi bị chính sách từ chối lặng lẽ, để ô mở là mời người dùng gõ một thứ không bao giờ ăn.
            if (_model.IsEndLocked)
            {
                field.SetEnabled(false);
                field.tooltip = _model.EndLockReason;
            }
            else
            {
                field.RegisterValueChangedCallback(change =>
                {
                    if (change.newValue <= 0 || !entry.TryGetStartUtc(out DateTime startUtc)) return;
                    FixedLiveEventEntry next = entry.WithTimes(entry.StartUtcText,
                        LiveEventUtcText.Format(startUtc.AddHours(change.newValue)));
                    string message = _presenter.DragToastMessage(entry, next);
                    _presenter.ApplyEdit(new ReplaceFixedEventEdit(next), LiveOpsEditOperation.ChangeFixedEventTimes, entry.EntryKey,
                        message, string.Empty, _presenter.DragUndoStepName(entry, next));
                });
            }
            row.Add(field);
            row.Add(new Label(LiveOpsHubStrings.CalendarDurationUnitLabel));
            row.Add(AddEventPopover.BuildDurationNote(_model.DurationHours, _services.Format));
            return row;
        }

        private VisualElement BuildConfigKeyField(FixedLiveEventEntry entry)
        {
            VisualElement row = BuildFieldRow();
            TextField field = new TextField(LiveOpsHubStrings.CalendarFieldConfigKeyLabel)
            {
                value = entry.ConfigKey,
                isDelayed = true,
            };
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorField);
            field.AddToClassList(LiveOpsHubClassNames.CalendarInspectorFieldText);
            field.AddToClassList(LiveOpsHubClassNames.Mono);
            LiveOpsPlaceholder hint = LiveOpsPlaceholder.Attach(field, _model.ConfigKeyPlaceholder);
            hint.AddToClassList(LiveOpsHubClassNames.CalendarFlowHint);
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
            VisualElement row = BuildReadOnlyRow(LiveOpsHubStrings.CalendarFieldRequiresOptInLabel, _model.RequiresOptInText);
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
            return BuildReadOnlyRow(LiveOpsHubStrings.CalendarFieldSourceLabel, _model.SourceText);
        }

        /// <summary>
        /// (W9-UX09-FOLDOUT) Card "Vấn đề" KHÔNG còn <c>viewDataKey</c>. Vì sao: khoá ấy dùng CHUNG cho mọi đợt, mà viewData
        /// khôi phục trạng thái gập SAU khi element gắn vào panel — tức nó ghi đè <c>value</c> vừa đặt theo dữ liệu. Hệ quả:
        /// xem một đợt sạch (card gập vì không có vấn đề) rồi chọn đợt HỎNG thì card cũng mở ra gập, và người dùng không thấy
        /// vấn đề nào trừ khi tự bấm. Khoá theo từng đợt chỉ dời lỗi đi một bước (đợt mới vẫn thừa kế khoá rỗng và vẫn đua với
        /// lượt gắn panel); trạng thái đúng của card suy được từ dữ liệu — CÓ vấn đề thì mở — nên không cần nhớ gì cả, và
        /// inspector dựng lại toàn bộ mỗi lần đổi lựa chọn.
        /// </summary>
        private VisualElement BuildIssuesFoldout()
        {
            Foldout foldout = new Foldout
            {
                text = _model.IssuesFoldoutText,
                value = _model.Findings.Count > 0,
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
            // Hàng phát hiện của màn Kiểm lịch là hàng NGANG (khối nút bên phải). Trong pane 280px dùng nguyên xi thì headline
            // cụt và hai nút đề xuất nằm ngoài pane — mất hẳn đường sửa lỗi ngay tại inspector (C4).
            card.AddToClassList(LiveOpsHubClassNames.CalendarFindingCardStacked);
            LiveOpsHubStyle.SetSeverityStripe(card, LiveOpsHubFindingRouting.StateOf(finding.Consequence));

            LiveEventCalendarDocument document = _services.Session.Document ?? LiveEventCalendarDocument.Empty;
            Label headline = new Label(LiveOpsFindingText.Headline(finding, _services.Format, document.LatestStamp));
            headline.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            LiveOpsHubStyle.SetStateText(headline, LiveOpsHubFindingRouting.StateOf(finding.Consequence));
            card.Add(headline);

            Label meta = new Label(LiveOpsFindingText.Meta(finding, _services.Format, _services.Clock.UtcNow));
            meta.AddToClassList(LiveOpsHubClassNames.Caption);
            meta.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
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
            button.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            // (R-F2) Card "Vấn đề" xếp DỌC với align-items: stretch, nên nút đề xuất lấy trọn bề ngang card. Nút xuống
            // dòng vẫn gói đúng trong card nhờ trần max-width: 100% của chính class này.
            button.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
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
            button.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            // (R-F2) Nút "Sửa thành …" nằm trong unity-content của foldout — hộp xếp DỌC, nên nó cũng lấy trọn bề ngang
            // pane. Lượt đầu để lọt vì lưới miễn trừ mọi nút white-space: normal; miễn trừ ấy nay đo gói dòng THẬT.
            button.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
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
            button.AddToClassList(LiveOpsHubClassNames.CalendarWrapText);
            button.AddToClassList(LiveOpsHubClassNames.ButtonDanger);
            // (J3-01) Nút "Xoá đợt…" đứng trong cột của inspector — đo được 265px (pane 1280) và 264px (pane 820) trước
            // bản vá, tức trọn bề ngang pane trừ đệm.
            button.AddToClassList(LiveOpsHubClassNames.ButtonSelfStart);
            return button;
        }
    }
}
