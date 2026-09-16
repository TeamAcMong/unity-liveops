using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Card "Cổng xuất" [SD2 §3.4]: vẽ lại đúng <see cref="ExportGateState"/> của G-EXPORTGATE (V-9) — năm dòng cao 22px
    /// (dấu 8px + chữ + meta / nút / chevron), hoặc MỘT dòng dạng gọn khi bốn điều kiện chặn được đều đạt (PD-11).
    /// <para>
    /// Card không tự nghĩ chữ và không tự quyết trạng thái: mọi câu, mọi dấu và mọi việc của dòng đều nằm trong state. Nhờ
    /// vậy <c>GateCard_RendersEveryGateState</c> vẽ được cả mười một mã trạng thái mà không cần dựng mười một phiên khác nhau.
    /// </para>
    /// </summary>
    internal sealed class ExportGateCard : VisualElement
    {
        internal const string ElementName = "export-gate-card";
        internal const string RowsElementName = "export-gate-rows";
        internal const string CompactElementName = "export-gate-compact";
        internal const string MetaElementName = "export-gate-meta";

        private readonly Label _title = new Label(LiveOpsHubStrings.ExportGateCardTitle);
        private readonly Label _meta = new Label { name = MetaElementName };
        private readonly VisualElement _rows = new VisualElement { name = RowsElementName };
        private readonly VisualElement _compactRow = new VisualElement { name = CompactElementName };
        private readonly LiveOpsStateMark _compactMark = new LiveOpsStateMark();
        private readonly Label _compactText = new Label();
        private readonly Label _compactRemote = new Label();

        private bool _canPasteRunningJson = true;
        private string _pasteUnavailableReason = string.Empty;

        public ExportGateCard()
        {
            name = ElementName;
            AddToClassList(LiveOpsHubClassNames.Card);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CardHeader);
            _title.AddToClassList(LiveOpsHubClassNames.ExportCardTitle);
            _meta.AddToClassList(LiveOpsHubClassNames.ExportCardMeta);
            header.Add(_title);
            header.Add(_meta);
            Add(header);

            _compactRow.AddToClassList(LiveOpsHubClassNames.ExportGateCompactRow);
            _compactMark.Size = LiveOpsStateMark.MarkSize.Small;
            _compactText.AddToClassList(LiveOpsHubClassNames.ExportGateRowText);
            _compactRemote.AddToClassList(LiveOpsHubClassNames.ExportGateCompactRemote);
            _compactRow.Add(_compactMark);
            _compactRow.Add(_compactText);
            _compactRow.Add(_compactRemote);
            Add(_compactRow);

            Add(_rows);
        }

        /// <summary>Dòng cổng được bấm (hoặc nút của dòng) — section nối việc thật; card không biết cửa sổ.</summary>
        public event Action<ExportGateRow> RowActivated;

        internal Label MetaLabel => _meta;
        internal VisualElement Rows => _rows;
        internal VisualElement CompactRow => _compactRow;
        internal Label CompactText => _compactText;

        /// <summary>Số dòng đang vẽ (0 ở dạng gọn) — test đếm mà không phải đi vào cây con.</summary>
        internal int VisibleRowCount => _rows.childCount;

        /// <summary>Dựng lại card khi mọi action của dòng cổng đều dùng được (test Logic của card).</summary>
        public void Bind(ExportGateState state)
        {
            Bind(state, true, string.Empty);
        }

        /// <param name="canPasteRunningJson">
        /// Action "Dán JSON đang chạy…" của dòng cổng 4 có dùng được không (<c>services.Actions</c>). false thì nút vẽ KHOÁ kèm
        /// lý do — vẽ nút bật rồi để cú bấm rơi vào no-op là nói dối người dùng (mục 12 I-3).
        /// </param>
        /// <param name="pasteUnavailableReason">Lý do in cạnh nút khi action không dùng được; bắt buộc khi khoá.</param>
        public void Bind(ExportGateState state, bool canPasteRunningJson, string pasteUnavailableReason)
        {
            if (state == null) throw new ArgumentNullException(nameof(state), LiveOpsHubStrings.ExportGateErrorInputMissing);
            _canPasteRunningJson = canPasteRunningJson;
            _pasteUnavailableReason = pasteUnavailableReason ?? string.Empty;

            _meta.text = state.CardMetaText;
            _meta.EnableInClassList(LiveOpsHubClassNames.ExportHidden, state.CardMetaText.Length == 0);
            _rows.Clear();

            _compactRow.EnableInClassList(LiveOpsHubClassNames.ExportHidden, !state.IsCompact);
            _rows.EnableInClassList(LiveOpsHubClassNames.ExportHidden, state.IsCompact);
            if (state.IsCompact)
            {
                _compactMark.SetHealth(HealthState.Ok);
                _compactText.text = state.CompactText;
                _compactRemote.text = state.CompactRemoteText;
                _compactRemote.EnableInClassList(LiveOpsHubClassNames.ExportHidden, state.CompactRemoteText.Length == 0);
                return;
            }

            foreach (ExportGateRow row in state.Rows)
            {
                if (!row.IsVisible) continue;
                _rows.Add(BuildRow(row));
            }
        }

        private VisualElement BuildRow(ExportGateRow row)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(LiveOpsHubClassNames.ExportGateRow);
            if (row.State == ExportGateRowState.Running) element.AddToClassList(LiveOpsHubClassNames.ExportGateRowRunning);

            if (row.State == ExportGateRowState.Running)
            {
                // Spinner 12px NGAY TRONG hàng cổng [SD2 §3.4]: kiểm lại không khoá cửa sổ, nên dấu trạng thái ở đây là chuyển động.
                LiveOpsSpinner spinner = new LiveOpsSpinner();
                spinner.Start();
                element.Add(spinner);
            }
            else
            {
                LiveOpsStateMark mark = new LiveOpsStateMark();
                mark.Size = LiveOpsStateMark.MarkSize.Small;
                mark.SetHealth(HealthOf(row.State));
                element.Add(mark);
            }

            Label text = new Label(row.Text);
            text.AddToClassList(LiveOpsHubClassNames.ExportGateRowText);
            if (row.State == ExportGateRowState.Blocked) LiveOpsHubStyle.SetStateText(text, HealthState.Blocked);
            element.Add(text);

            if (row.MetaText.Length > 0)
            {
                Label meta = new Label(row.MetaText);
                meta.AddToClassList(LiveOpsHubClassNames.ExportGateRowMeta);
                element.Add(meta);
            }

            VisualElement spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList(LiveOpsHubClassNames.ExportGateRowSpacer);
            element.Add(spacer);

            if (row.Action != ExportGateRowAction.None && row.ActionText.Length > 0)
            {
                Button action = new Button(() => OnRowActivated(row)) { text = row.ActionText };
                action.AddToClassList(LiveOpsHubClassNames.Button);
                if (row.Action == ExportGateRowAction.PasteRunningJson && !_canPasteRunningJson && _pasteUnavailableReason.Length > 0)
                {
                    LiveOpsButtonSlot slot = new LiveOpsButtonSlot(action);
                    slot.SetEnabledWithReason(false, _pasteUnavailableReason);
                    element.Add(slot);
                }
                else
                {
                    element.Add(action);
                }
            }
            else if (row.Navigation != null)
            {
                // Cả dòng bấm được (meta card nói "bấm một dòng để tới màn liên quan") — chevron chỉ là dấu hiệu.
                element.AddToClassList(LiveOpsHubClassNames.ExportGateRowClickable);
                element.Add(new LiveOpsChevron());
                element.RegisterCallback<ClickEvent>(clickEvent => OnRowActivated(row));
            }

            return element;
        }

        private void OnRowActivated(ExportGateRow row)
        {
            Action<ExportGateRow> handler = RowActivated;
            if (handler != null) handler(row);
        }

        private static HealthState HealthOf(ExportGateRowState state)
        {
            switch (state)
            {
                case ExportGateRowState.Ok: return HealthState.Ok;
                case ExportGateRowState.Warning: return HealthState.Warning;
                case ExportGateRowState.Blocked: return HealthState.Blocked;
                default: return HealthState.NotMeasured;
            }
        }
    }
}
