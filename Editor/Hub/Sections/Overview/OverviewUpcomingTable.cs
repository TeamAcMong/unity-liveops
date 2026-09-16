using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View mỏng của bảng "7 ngày tới" [SD1 §1.1]: cột 170 / 250 / 140 / 130 / giãn — Lúc (UTC) | Đợt | Loại | Sự kiện | Ghi chú.
    /// Cột Lúc là UTC tuyệt đối (mono) với dòng phụ tương đối 10px; cột Loại là swatch 9×9 + id loại; đợt chỉ có trong nháp mang
    /// tag "chỉ trong nháp". Bảng tự dựng bằng hàng VisualElement thay vì MultiColumnListView: 8–12 dòng không cần ảo hoá, và
    /// một dòng có hai mức chữ (giờ + tương đối) nên ô cột cố định của MultiColumnListView không hợp.
    /// </summary>
    internal sealed class OverviewUpcomingTable
    {
        internal const string HeadRowElementName = "overview-upcoming-head";
        internal const string RowElementNamePrefix = "overview-upcoming-row-";
        internal const string EmptyElementName = "overview-upcoming-empty";
        internal const string HeadCellElementNamePrefix = "overview-upcoming-col-";

        private readonly VisualElement _container;

        internal OverviewUpcomingTable(VisualElement container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        internal void Rebuild(IReadOnlyList<OverviewUpcomingRow> rows)
        {
            _container.Clear();
            _container.Add(BuildHeadRow());
            if (rows == null || rows.Count == 0)
            {
                _container.Add(BuildEmpty());
                return;
            }
            int index = 0;
            foreach (OverviewUpcomingRow row in rows)
            {
                _container.Add(BuildRow(row, index));
                index++;
            }
        }

        private static VisualElement BuildHeadRow()
        {
            VisualElement head = new VisualElement { name = HeadRowElementName };
            head.AddToClassList(LiveOpsHubClassNames.OverviewUpcomingHeadRow);
            head.Add(BuildHeadCell(LiveOpsHubStrings.OverviewUpcomingColumnTime, LiveOpsHubClassNames.OverviewUpcomingCellTime, 0));
            head.Add(BuildHeadCell(LiveOpsHubStrings.OverviewUpcomingColumnEvent, LiveOpsHubClassNames.OverviewUpcomingCellEvent, 1));
            head.Add(BuildHeadCell(LiveOpsHubStrings.OverviewUpcomingColumnType, LiveOpsHubClassNames.OverviewUpcomingCellType, 2));
            head.Add(BuildHeadCell(LiveOpsHubStrings.OverviewUpcomingColumnKind, LiveOpsHubClassNames.OverviewUpcomingCellKind, 3));
            head.Add(BuildHeadCell(LiveOpsHubStrings.OverviewUpcomingColumnNote, LiveOpsHubClassNames.OverviewUpcomingCellNote, 4));
            return head;
        }

        /// <summary>Ô tiêu đề có TÊN để lệnh chụp ghi worldBound: bề rộng cột 170/250/140/130 là số đo của ma trận 9.5.</summary>
        private static VisualElement BuildHeadCell(string text, string columnClass, int columnIndex)
        {
            VisualElement cell = BuildCell(columnClass);
            cell.name = HeadCellElementNamePrefix + columnIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Label label = new Label(text);
            label.AddToClassList(LiveOpsHubClassNames.Caption);
            cell.Add(label);
            return cell;
        }

        private static VisualElement BuildCell(string columnClass)
        {
            VisualElement cell = new VisualElement();
            cell.AddToClassList(LiveOpsHubClassNames.OverviewUpcomingCell);
            cell.AddToClassList(columnClass);
            return cell;
        }

        private static VisualElement BuildEmpty()
        {
            VisualElement empty = new VisualElement { name = EmptyElementName };
            empty.AddToClassList(LiveOpsHubClassNames.Empty);
            Label title = new Label(LiveOpsHubStrings.OverviewUpcomingEmptyTitle);
            title.AddToClassList(LiveOpsHubClassNames.EmptyTitle);
            empty.Add(title);
            // Trống không phải là đạt: nói vì sao trống, không để một bảng trắng ([FD §4]).
            Label body = new Label(LiveOpsHubStrings.OverviewUpcomingEmptyBody);
            body.AddToClassList(LiveOpsHubClassNames.EmptyBody);
            empty.Add(body);
            return empty;
        }

        private static VisualElement BuildRow(OverviewUpcomingRow row, int index)
        {
            VisualElement element = new VisualElement
            {
                name = RowElementNamePrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            element.AddToClassList(LiveOpsHubClassNames.OverviewUpcomingRow);

            VisualElement timeCell = BuildCell(LiveOpsHubClassNames.OverviewUpcomingCellTime);
            Label time = new Label(row.TimeText);
            time.AddToClassList(LiveOpsHubClassNames.Mono);
            timeCell.Add(time);
            if (row.RelativeText.Length > 0)
            {
                Label relative = new Label(row.RelativeText);
                relative.AddToClassList(LiveOpsHubClassNames.OverviewUpcomingRelative);
                timeCell.Add(relative);
            }
            element.Add(timeCell);

            VisualElement eventCell = BuildCell(LiveOpsHubClassNames.OverviewUpcomingCellEvent);
            Label eventId = new Label(row.EventIdText);
            eventId.AddToClassList(LiveOpsHubClassNames.Mono);
            eventCell.Add(eventId);
            if (row.IsDraftOnly)
            {
                Label tag = new Label(LiveOpsHubStrings.OverviewUpcomingDraftOnlyTag);
                tag.AddToClassList(LiveOpsHubClassNames.Tag);
                eventCell.Add(tag);
            }
            element.Add(eventCell);

            VisualElement typeCell = BuildCell(LiveOpsHubClassNames.OverviewUpcomingCellType);
            VisualElement swatch = new VisualElement();
            swatch.AddToClassList(LiveOpsHubClassNames.Swatch);
            LiveOpsHubStyle.SetEventColor(swatch, row.ColorSlot);
            typeCell.Add(swatch);
            typeCell.Add(new Label(row.EventType));
            element.Add(typeCell);

            VisualElement kindCell = BuildCell(LiveOpsHubClassNames.OverviewUpcomingCellKind);
            if (row.Kind == OverviewUpcomingKind.Dropped)
            {
                // Đợt bị game bỏ mang dấu Blocked ngay trong bảng: bảng 7 ngày là chỗ người đọc thấy hậu quả sớm nhất.
                LiveOpsStateMark mark = new LiveOpsStateMark();
                mark.Size = LiveOpsStateMark.MarkSize.Small;
                mark.SetHealth(HealthState.Blocked);
                kindCell.Add(mark);
            }
            Label kind = new Label(row.KindText);
            if (row.Kind == OverviewUpcomingKind.Dropped) LiveOpsHubStyle.SetStateText(kind, HealthState.Blocked);
            kindCell.Add(kind);
            element.Add(kindCell);

            VisualElement noteCell = BuildCell(LiveOpsHubClassNames.OverviewUpcomingCellNote);
            Label note = new Label(row.NoteText);
            if (row.NoteState.HasValue) LiveOpsHubStyle.SetStateText(note, row.NoteState.Value);
            noteCell.Add(note);
            element.Add(noteCell);
            return element;
        }
    }
}
