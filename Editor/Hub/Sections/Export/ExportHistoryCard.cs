using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Card "Các lần đã đăng" [SD2 §3.9]: bốn cột (Lúc (UTC) 90px mono · Người 110px · Ghi chú co giãn · sha 60px mono),
    /// hàng Active có viền trái 3px và đuôi "· đang là bản so", menu chuột phải So với nháp · Khôi phục vào nháp… ·
    /// Gỡ dấu này… (chỉ lần mới nhất).
    /// <para>
    /// Bảng dựng bằng hàng thường chứ không bằng <c>MultiColumnListView</c>: danh sách dấu đã đăng đếm bằng chục, còn
    /// <c>MultiColumnListView</c> ảo hoá hàng nên hàng ngoài khung không có element để test và ảnh chụp kiểm.
    /// </para>
    /// </summary>
    internal sealed class ExportHistoryCard : VisualElement
    {
        internal const string ElementName = "export-history-card";
        internal const string TableElementName = "export-history-table";

        private readonly VisualElement _table = new VisualElement { name = TableElementName };

        public ExportHistoryCard()
        {
            name = ElementName;
            AddToClassList(LiveOpsHubClassNames.Card);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CardHeader);
            Label title = new Label(LiveOpsHubStrings.ExportHistoryCardTitle);
            title.AddToClassList(LiveOpsHubClassNames.ExportCardTitle);
            Label meta = new Label(LiveOpsHubStrings.ExportHistoryCardMeta);
            meta.AddToClassList(LiveOpsHubClassNames.ExportCardMeta);
            header.Add(title);
            header.Add(meta);
            Add(header);

            _table.AddToClassList(LiveOpsHubClassNames.ExportHistoryTable);
            Add(_table);
        }

        public event Action<PublishedCalendarStamp> CompareRequested;
        public event Action<PublishedCalendarStamp> RestoreRequested;
        public event Action<PublishedCalendarStamp> RemoveStampRequested;

        internal VisualElement Table => _table;

        /// <summary>Số hàng dấu đang vẽ (không tính hàng tiêu đề) — test đếm mà không đi vào cây con.</summary>
        internal int RowCount => _table.childCount == 0 ? 0 : _table.childCount - 1;

        public void Bind(ExportHistoryModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model), LiveOpsHubStrings.ExportGateErrorInputMissing);

            _table.Clear();
            _table.Add(BuildHeadRow());
            if (model.IsEmpty)
            {
                Label empty = new Label(model.EmptyText);
                empty.AddToClassList(LiveOpsHubClassNames.ExportDiffEmpty);
                _table.Add(empty);
                return;
            }

            foreach (ExportHistoryRow row in model.Rows) _table.Add(BuildRow(row));
        }

        private static VisualElement BuildHeadRow()
        {
            VisualElement head = new VisualElement();
            head.AddToClassList(LiveOpsHubClassNames.ExportHistoryHeadRow);
            head.Add(BuildCell(LiveOpsHubStrings.ExportHistoryColumnTime, LiveOpsHubClassNames.ExportHistoryCellTime, false));
            head.Add(BuildCell(LiveOpsHubStrings.ExportHistoryColumnPublisher, LiveOpsHubClassNames.ExportHistoryCellPublisher, false));
            head.Add(BuildCell(LiveOpsHubStrings.ExportHistoryColumnNote, LiveOpsHubClassNames.ExportHistoryCellNote, false));
            head.Add(BuildCell(LiveOpsHubStrings.ExportHistoryColumnSha, LiveOpsHubClassNames.ExportHistoryCellSha, false));
            return head;
        }

        private VisualElement BuildRow(ExportHistoryRow row)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(LiveOpsHubClassNames.ExportHistoryRow);
            // "Đang là bản so" dùng class Active dùng chung (8.10) — Active khác Selected (nền) và Focus (viền 1px).
            if (row.IsActiveBaseline) element.AddToClassList(LiveOpsHubClassNames.RowActive);

            element.Add(BuildCell(row.TimeText, LiveOpsHubClassNames.ExportHistoryCellTime, true));
            element.Add(BuildCell(row.PublisherText, LiveOpsHubClassNames.ExportHistoryCellPublisher, false));

            VisualElement noteCell = new VisualElement();
            noteCell.AddToClassList(LiveOpsHubClassNames.ExportHistoryCell);
            noteCell.AddToClassList(LiveOpsHubClassNames.ExportHistoryCellNote);
            noteCell.Add(new Label(row.NoteText));
            if (row.IsActiveBaseline)
            {
                Label activeSuffix = new Label(LiveOpsHubStrings.ExportHistoryActiveSuffix);
                activeSuffix.AddToClassList(LiveOpsHubClassNames.ExportHistoryActiveSuffix);
                noteCell.Add(activeSuffix);
            }
            element.Add(noteCell);

            element.Add(BuildCell(row.ShaText, LiveOpsHubClassNames.ExportHistoryCellSha, true));

            element.AddManipulator(new ContextualMenuManipulator(menuEvent => PopulateMenu(menuEvent, row)));
            return element;
        }

        private void PopulateMenu(ContextualMenuPopulateEvent menuEvent, ExportHistoryRow row)
        {
            menuEvent.menu.AppendAction(LiveOpsHubStrings.ExportHistoryMenuCompare, action => Raise(CompareRequested, row.Stamp));
            menuEvent.menu.AppendAction(LiveOpsHubStrings.ExportHistoryMenuRestore, action => Raise(RestoreRequested, row.Stamp));
            // Gỡ dấu chỉ ở lần mới nhất: mục vẫn hiện nhưng mờ, để người dùng thấy nó tồn tại và hiểu vì sao không bấm được.
            menuEvent.menu.AppendAction(LiveOpsHubStrings.ExportHistoryMenuRemoveStamp, action => Raise(RemoveStampRequested, row.Stamp),
                action => row.CanRemoveStamp ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        private static void Raise(Action<PublishedCalendarStamp> handler, PublishedCalendarStamp stamp)
        {
            if (handler != null) handler(stamp);
        }

        private static VisualElement BuildCell(string text, string columnClassName, bool isMono)
        {
            Label cell = new Label(text);
            cell.AddToClassList(LiveOpsHubClassNames.ExportHistoryCell);
            cell.AddToClassList(columnClassName);
            if (isMono) cell.AddToClassList(LiveOpsHubClassNames.Mono);
            return cell;
        }
    }
}
