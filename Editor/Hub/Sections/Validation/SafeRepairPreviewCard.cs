using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>Một lệnh sửa an toàn đang chờ áp: phát hiện sinh ra nó + chính lệnh sửa.</summary>
    internal sealed class SafeRepairPreviewItem
    {
        internal SafeRepairPreviewItem(LiveEventCalendarFinding finding, LiveEventCalendarRepair repair)
        {
            Finding = finding ?? throw new ArgumentNullException(nameof(finding));
            Repair = repair ?? throw new ArgumentNullException(nameof(repair));
        }

        internal LiveEventCalendarFinding Finding { get; }
        internal LiveEventCalendarRepair Repair { get; }
    }

    /// <summary>
    /// Card xem trước sửa hàng loạt ([SD2 §2.5], Hình 17 ô 1) — chen NGAY DƯỚI dải summary khi bấm
    /// "Sửa các lỗi an toàn (n)…". Ba luật của thiết kế nằm ở đây:
    /// <list type="bullet">
    /// <item>Là CARD, không phải popover và không phải hộp modal: người dùng vẫn đọc được dải summary và danh sách phát hiện
    /// trong lúc soát từng thay đổi.</item>
    /// <item>Mỗi thay đổi một Toggle bật sẵn, tắt được từng cái; mọi nhãn đếm (header, dòng Undo group, nút Áp) đi theo số mục
    /// ĐANG BẬT — không theo tổng số lệnh sửa.</item>
    /// <item>"Đề xuất…" KHÔNG BAO GIỜ nằm ở đây ([SD2 §2.4]), kể cả khi có mười đề xuất giống nhau: card chỉ nhận lệnh sửa
    /// <see cref="LiveEventCalendarRepairKind.SafeRepair"/>, và người gọi lọc trước.</item>
    /// </list>
    /// Card KHÔNG tự sửa lịch: nó phát <see cref="ApplyRequested"/> kèm đúng danh sách đang bật để màn quyết một Undo group,
    /// một toast rồi tự kiểm lại (PD-10) — cùng đường với mọi lệnh sửa khác.
    /// </summary>
    internal sealed class SafeRepairPreviewCard : VisualElement
    {
        private readonly List<SafeRepairPreviewItem> _items = new List<SafeRepairPreviewItem>();
        private readonly List<Toggle> _toggles = new List<Toggle>();
        private readonly LiveOpsHubFormat _format;
        private readonly Label _title;
        private readonly Label _undoLine;
        private readonly LiveOpsButtonSlot _applySlot;

        internal SafeRepairPreviewCard(IReadOnlyList<SafeRepairPreviewItem> items, LiveOpsHubFormat format)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            _format = format ?? throw new ArgumentNullException(nameof(format));
            for (int index = 0; index < items.Count; index++)
            {
                _items.Add(items[index]);
            }

            name = LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewCard;
            AddToClassList(LiveOpsHubClassNames.Card);
            AddToClassList(LiveOpsHubClassNames.ValidationBulkPreview);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CardHeader);
            _title = new Label { name = LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewTitle };
            _title.AddToClassList(LiveOpsHubClassNames.ValidationGroupTitle);
            header.Add(_title);
            Label meta = new Label(LiveOpsHubStrings.ValidationDepthBulkPreviewMeta);
            meta.AddToClassList(LiveOpsHubClassNames.ValidationBulkPreviewMeta);
            header.Add(meta);
            Add(header);

            BuildRows();

            _undoLine = new Label { name = LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewUndoLine };
            _undoLine.AddToClassList(LiveOpsHubClassNames.ValidationBulkPreviewUndo);
            Add(_undoLine);

            VisualElement footer = new VisualElement();
            footer.AddToClassList(LiveOpsHubClassNames.ValidationBulkPreviewFooter);
            CancelButton = new Button(() => CancelRequested?.Invoke())
            {
                name = LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewCancel,
                text = LiveOpsHubStrings.ValidationDepthBulkPreviewCancelButton,
            };
            CancelButton.AddToClassList(LiveOpsHubClassNames.Button);
            footer.Add(CancelButton);

            Button apply = new Button(OnApplyClicked) { name = LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewApply };
            apply.AddToClassList(LiveOpsHubClassNames.Button);
            apply.AddToClassList(LiveOpsHubClassNames.ButtonPrimary);
            _applySlot = new LiveOpsButtonSlot(apply);
            footer.Add(_applySlot);
            Add(footer);

            RefreshCounts();
        }

        /// <summary>Người dùng bấm "Áp n thay đổi"; tham số là đúng những mục đang bật, theo thứ tự hàng.</summary>
        internal event Action<IReadOnlyList<SafeRepairPreviewItem>> ApplyRequested;

        internal event Action CancelRequested;

        /// <summary>Số mục đang bật đổi — nhãn nút "Sửa các lỗi an toàn (n)…" của section header cũng đếm theo số này.</summary>
        internal event Action<int> SelectedCountChanged;

        internal Button CancelButton { get; }
        internal Button ApplyButton => _applySlot.Button;
        internal Label TitleLabel => _title;
        internal Label UndoLine => _undoLine;
        internal IReadOnlyList<Toggle> Toggles => _toggles;
        internal int ItemCount => _items.Count;

        /// <summary>Số mục đang bật; nhãn đếm và danh sách áp đều đọc từ đây.</summary>
        internal int SelectedCount
        {
            get
            {
                int count = 0;
                for (int index = 0; index < _toggles.Count; index++)
                {
                    if (_toggles[index].value) count++;
                }
                return count;
            }
        }

        /// <summary>Tên Undo group mà lần Áp hiện tại sẽ tạo — dòng 10px của card in đúng chuỗi này, không in một bản gần giống.</summary>
        internal string UndoGroupName
        {
            get { return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationSafeRepairUndoFormat, SelectedCount); }
        }

        private void BuildRows()
        {
            for (int index = 0; index < _items.Count; index++)
            {
                VisualElement row = new VisualElement
                {
                    name = LiveOpsHubPaths.ValidationDepthElementNames.BulkPreviewRowPrefix + index.ToString(CultureInfo.InvariantCulture),
                };
                row.AddToClassList(LiveOpsHubClassNames.ValidationBulkPreviewRow);

                Toggle toggle = new Toggle { value = true };
                toggle.RegisterValueChangedCallback(changeEvent => RefreshCounts());
                row.Add(toggle);
                _toggles.Add(toggle);

                Label change = new Label(ChangeTextOf(_items[index])) { enableRichText = true };
                change.AddToClassList(LiveOpsHubClassNames.ValidationBulkPreviewChange);
                change.AddToClassList(LiveOpsHubClassNames.Mono);
                row.Add(change);
                Add(row);
            }
        }

        /// <summary>
        /// Chữ mono của một hàng ([SD2 §2.5]): "lava-quest-2026-10 endUtc "2026-10-3" → "2026-10-03T00:00:00Z"". Tên trường suy
        /// từ biến thể của phát hiện — người soát phải biết mình sắp ghi đè Ô NÀO, "id + hai giá trị" là chưa đủ khi một đợt có
        /// hai mốc giờ. Luật của game tự viết có thể không nói được trường nào; khi đó bỏ hẳn vế tên trường thay vì đoán.
        /// </summary>
        private string ChangeTextOf(SafeRepairPreviewItem item)
        {
            string before = LiveOpsFindingText.Quoted(item.Repair.BeforeText);
            string after = LiveOpsFindingText.Quoted(item.Repair.AfterText);
            string fieldName = FieldNameOf(item.Finding.DetailCode);
            if (fieldName.Length == 0)
            {
                return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthBulkPreviewChangeNoFieldFormat,
                    LiveOpsFindingText.NoParse(item.Finding.TargetId), before, after);
            }
            return string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthBulkPreviewChangeFormat,
                LiveOpsFindingText.NoParse(item.Finding.TargetId), LiveOpsFindingText.NoParse(fieldName), before, after);
        }

        /// <summary>Tên trường JSON, không phải câu cho người đọc: đúng khoá mà người dùng thấy khi mở file lịch.</summary>
        private static string FieldNameOf(string detailCode)
        {
            switch (detailCode)
            {
                case LiveEventCalendarDetailCodes.StartUnreadable: return StartFieldName;
                case LiveEventCalendarDetailCodes.EndUnreadable: return EndFieldName;
                case LiveEventCalendarDetailCodes.AnchorUnreadable: return AnchorFieldName;
                default: return string.Empty;
            }
        }

        private const string StartFieldName = "startUtc";
        private const string EndFieldName = "endUtc";
        private const string AnchorFieldName = "anchorUtc";

        private void RefreshCounts()
        {
            int selectedCount = SelectedCount;
            _title.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthBulkPreviewTitleFormat, selectedCount);
            _undoLine.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthBulkPreviewUndoFormat, UndoGroupName);
            _applySlot.Button.text = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.ValidationDepthBulkPreviewApplyFormat, selectedCount);
            // SPIKE-B SP-3: lý do nút bị khoá LUÔN in thành chữ cạnh nút, tooltip chỉ phụ.
            _applySlot.SetEnabledWithReason(selectedCount > 0, selectedCount > 0 ? string.Empty : LiveOpsHubStrings.ValidationDepthBulkPreviewNothingReason);
            SelectedCountChanged?.Invoke(selectedCount);
        }

        private void OnApplyClicked()
        {
            var selected = new List<SafeRepairPreviewItem>();
            for (int index = 0; index < _items.Count && index < _toggles.Count; index++)
            {
                if (_toggles[index].value) selected.Add(_items[index]);
            }
            if (selected.Count == 0) return;
            ApplyRequested?.Invoke(selected);
        }
    }
}
