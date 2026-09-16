using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Card "5 đợt kế tiếp" ([SD1 §4.1]): bảng Id | Bắt đầu UTC | Kết thúc UTC | Giờ máy | Lúc này, chân card có nút
    /// "Thêm 5" nối thêm từng 5 lần tới trần 50.
    /// <para>
    /// Hai quy ước của thiết kế được giữ nguyên ở đây vì chúng là lý do bảng đọc được: (1) id "cũ → mới" in bằng BA Label —
    /// mono, mũi tên Inter, mono — vì RobotoMono không có glyph →; (2) cột "Lúc này" dùng dấu giai đoạn, còn việc đổi id là
    /// một TAG riêng có thoi Warning, để hai họ hình không trộn vào một dấu.
    /// </para>
    /// </summary>
    internal sealed class RecurringNextOccurrencesTable : VisualElement
    {
        internal const string HeadElementName = "recurring-occurrences-head";
        internal const string RowsElementName = "recurring-occurrences-rows";
        internal const string AddMoreElementName = "recurring-occurrences-add-more";
        internal const string ComputingElementName = "recurring-occurrences-computing";

        private readonly Label _title;
        private readonly Label _subtitle;
        private readonly VisualElement _rows;
        private readonly Label _computing;
        private readonly Button _addMore;
        private readonly Label _limitNote;

        internal RecurringNextOccurrencesTable()
        {
            name = ElementName;
            AddToClassList(LiveOpsHubClassNames.Card);
            AddToClassList(LiveOpsHubClassNames.RecurringOccurrences);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CardHeader);
            _title = new Label();
            header.Add(_title);
            _subtitle = new Label();
            _subtitle.AddToClassList(LiveOpsHubClassNames.Caption);
            header.Add(_subtitle);
            Add(header);

            Add(BuildHead());

            _rows = new VisualElement { name = RowsElementName };
            Add(_rows);

            _computing = new Label(LiveOpsHubStrings.RecurringComputingNote) { name = ComputingElementName };
            _computing.AddToClassList(LiveOpsHubClassNames.Caption);
            Add(_computing);

            VisualElement foot = new VisualElement();
            foot.AddToClassList(LiveOpsHubClassNames.RecurringOccurrencesFoot);
            _addMore = new Button(RaiseAddMore) { name = AddMoreElementName };
            _addMore.AddToClassList(LiveOpsHubClassNames.Button);
            _addMore.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringAddMoreButtonFormat,
                RecurringRuleModel.OccurrenceCountStep);
            foot.Add(_addMore);
            _limitNote = new Label();
            _limitNote.AddToClassList(LiveOpsHubClassNames.Caption);
            foot.Add(_limitNote);
            Add(foot);

            ShowComputing(false);
        }

        internal const string ElementName = "recurring-occurrences-card";

        /// <summary>Người dùng bấm "Thêm 5".</summary>
        public event Action AddMoreRequested;

        public int RowCount => _rows.childCount;

        /// <summary>Chân card: nút "Thêm 5" tắt kèm câu "Đã hiện tối đa 50 đợt" khi chạm trần.</summary>
        public void SetRows(IReadOnlyList<RecurringOccurrenceRow> rows, string subtitleText, bool atLimit)
        {
            ShowComputing(false);
            _rows.Clear();
            _title.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringOccurrencesTitleFormat,
                rows != null ? rows.Count : 0);
            _subtitle.text = subtitleText ?? string.Empty;
            if (rows != null)
            {
                for (int index = 0; index < rows.Count; index++) _rows.Add(BuildRow(rows[index], index == rows.Count - 1));
            }
            _addMore.SetEnabled(!atLimit);
            _limitNote.text = atLimit
                ? string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.RecurringOccurrenceLimitNoteFormat,
                    RecurringRuleModel.MaximumOccurrenceCount)
                : string.Empty;
        }

        /// <summary>Trong lúc chờ debounce: chỉ "đang tính…", không hiện bảng cũ đã sai và không nói số mili giây.</summary>
        public void ShowComputing(bool isComputing)
        {
            _computing.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, !isComputing);
            _rows.EnableInClassList(LiveOpsHubClassNames.RecurringHidden, isComputing);
        }

        private VisualElement BuildHead()
        {
            VisualElement head = new VisualElement { name = HeadElementName };
            head.AddToClassList(LiveOpsHubClassNames.RecurringOccurrencesHead);
            head.Add(Cell(LiveOpsHubStrings.RecurringColumnId, LiveOpsHubClassNames.RecurringCellId));
            head.Add(Cell(LiveOpsHubStrings.RecurringColumnStartUtc, LiveOpsHubClassNames.RecurringCellStart));
            head.Add(Cell(LiveOpsHubStrings.RecurringColumnEndUtc, LiveOpsHubClassNames.RecurringCellEnd));
            head.Add(Cell(LiveOpsHubStrings.RecurringColumnDeviceTime, LiveOpsHubClassNames.RecurringCellDevice));
            head.Add(Cell(LiveOpsHubStrings.RecurringColumnNow, LiveOpsHubClassNames.RecurringCellNow));
            return head;
        }

        private static Label Cell(string text, string className)
        {
            Label label = new Label(text);
            label.AddToClassList(className);
            return label;
        }

        private static VisualElement BuildRow(RecurringOccurrenceRow row, bool isLast)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(LiveOpsHubClassNames.RecurringOccurrencesRow);
            element.EnableInClassList(LiveOpsHubClassNames.RowLast, isLast);

            VisualElement idCell = new VisualElement();
            idCell.AddToClassList(LiveOpsHubClassNames.RecurringCellId);
            if (row.IdChanges)
            {
                idCell.Add(MonoLabel(row.PublishedEventId));
                Label arrow = new Label(LiveOpsHubStrings.RecurringIdArrow);
                arrow.AddToClassList(LiveOpsHubClassNames.RecurringCellArrow);
                idCell.Add(arrow);
            }
            idCell.Add(MonoLabel(row.EventId));
            element.Add(idCell);

            element.Add(Cell(row.StartText, LiveOpsHubClassNames.RecurringCellStart));
            element.Add(Cell(row.EndText, LiveOpsHubClassNames.RecurringCellEnd));
            element.Add(Cell(row.DeviceTimeText, LiveOpsHubClassNames.RecurringCellDevice));

            VisualElement nowCell = new VisualElement();
            nowCell.AddToClassList(LiveOpsHubClassNames.RecurringCellNow);
            LiveOpsStateMark phaseMark = new LiveOpsStateMark { Kind = LiveOpsStateMark.MarkKind.Phase, Size = LiveOpsStateMark.MarkSize.Small };
            phaseMark.SetPhase(row.Phase, false);
            nowCell.Add(phaseMark);
            nowCell.Add(new Label(row.NowText));
            if (row.IdChanges)
            {
                Label tag = new Label(LiveOpsHubStrings.RecurringIdChangeTag);
                tag.AddToClassList(LiveOpsHubClassNames.Tag);
                tag.AddToClassList(LiveOpsHubClassNames.TextWarning);
                nowCell.Add(tag);
            }
            element.Add(nowCell);
            return element;
        }

        private static Label MonoLabel(string text)
        {
            Label label = new Label(text);
            label.AddToClassList(LiveOpsHubClassNames.Mono);
            return label;
        }

        private void RaiseAddMore()
        {
            if (AddMoreRequested != null) AddMoreRequested();
        }
    }
}
