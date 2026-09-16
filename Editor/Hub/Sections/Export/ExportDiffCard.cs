using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Card "So với bản đã đăng" [SD2 §3.8]: header + meta + bốn chip, rồi các nhóm hậu quả. Hàng hai dòng (tiêu đề ·
    /// câu hậu quả), ký hiệu mono đầu hàng, bên phải là Toggle "Đã xem" hoặc link "Sửa ở Kiểm lịch" của hàng Bị bỏ.
    /// <para>
    /// (V-13) Khi đang so với bản remote đã dán, card đổi header và KHÔNG vẽ Toggle nào — "Đã xem" luôn nói về dấu đã đăng.
    /// Chip "Về bản đã đăng" ở header trả nguồn bản so về Published.
    /// </para>
    /// </summary>
    internal sealed class ExportDiffCard : VisualElement
    {
        internal const string ElementName = "export-diff-card";
        internal const string TitleElementName = "export-diff-title";
        internal const string MetaElementName = "export-diff-meta";
        internal const string ChipsElementName = "export-diff-chips";
        internal const string BodyElementName = "export-diff-body";
        internal const string BackToPublishedElementName = "export-diff-back-to-published";

        private readonly Label _title = new Label { name = TitleElementName };
        private readonly Label _meta = new Label { name = MetaElementName };
        private readonly VisualElement _chips = new VisualElement { name = ChipsElementName };
        private readonly VisualElement _body = new VisualElement { name = BodyElementName };
        private readonly Button _backToPublished;

        public ExportDiffCard()
        {
            name = ElementName;
            AddToClassList(LiveOpsHubClassNames.Card);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CardHeader);
            _title.AddToClassList(LiveOpsHubClassNames.ExportCardTitle);
            _meta.AddToClassList(LiveOpsHubClassNames.ExportCardMeta);
            _backToPublished = new Button(RaiseBackToPublished)
            {
                name = BackToPublishedElementName,
                text = LiveOpsHubStrings.ExportCompareBackToPublishedChip,
            };
            _backToPublished.AddToClassList(LiveOpsHubClassNames.Chip);
            VisualElement spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList(LiveOpsHubClassNames.ExportCardSpacer);
            _chips.AddToClassList(LiveOpsHubClassNames.ExportDiffChipRow);
            header.Add(_title);
            header.Add(_meta);
            header.Add(_backToPublished);
            header.Add(spacer);
            header.Add(_chips);
            Add(header);

            Add(_body);
        }

        /// <summary>Toggle "Đã xem" của một mục đổi trạng thái.</summary>
        public event Action<LiveEventCalendarChange, bool> ReviewToggled;

        /// <summary>Link "Sửa ở Kiểm lịch" của một hàng Bị bỏ.</summary>
        public event Action<LiveEventCalendarChange> FixRequested;

        /// <summary>Chip "Về bản đã đăng" (V-13).</summary>
        public event Action BackToPublishedRequested;

        internal Label TitleLabel => _title;
        internal Label MetaLabel => _meta;
        internal VisualElement Body => _body;
        internal Button BackToPublishedChip => _backToPublished;

        public void Bind(ExportDiffViewModel model, bool showBackToPublished)
        {
            if (model == null) throw new ArgumentNullException(nameof(model), LiveOpsHubStrings.ExportGateErrorInputMissing);

            _title.text = model.HeaderText;
            _meta.text = model.MetaText;
            _meta.EnableInClassList(LiveOpsHubClassNames.ExportHidden, model.MetaText.Length == 0);
            _backToPublished.EnableInClassList(LiveOpsHubClassNames.ExportHidden, !showBackToPublished);

            _chips.Clear();
            foreach (string chipText in model.ChipTexts)
            {
                Label chip = new Label(chipText);
                chip.AddToClassList(LiveOpsHubClassNames.Chip);
                _chips.Add(chip);
            }

            _body.Clear();
            if (model.IsEmpty)
            {
                Label empty = new Label(model.EmptyText);
                empty.AddToClassList(LiveOpsHubClassNames.ExportDiffEmpty);
                _body.Add(empty);
                return;
            }

            bool isFirstGroup = true;
            foreach (ExportDiffGroup group in model.Groups)
            {
                _body.Add(BuildGroup(group, isFirstGroup));
                isFirstGroup = false;
            }
        }

        private VisualElement BuildGroup(ExportDiffGroup group, bool isFirstGroup)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(LiveOpsHubClassNames.ExportDiffGroup);
            // Nhóm sau có viền trên 1px [SD2 §3.8]; USS không có :nth-child nên class bật từ đây.
            if (!isFirstGroup) element.AddToClassList(LiveOpsHubClassNames.ExportDiffGroupLater);

            Label header = new Label(group.HeaderText);
            header.AddToClassList(LiveOpsHubClassNames.ExportDiffGroupHeader);
            element.Add(header);

            foreach (ExportDiffRow row in group.Rows) element.Add(BuildRow(row));
            return element;
        }

        private VisualElement BuildRow(ExportDiffRow row)
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(LiveOpsHubClassNames.ExportDiffRow);
            if (row.Consequence == LiveEventCalendarConsequence.Dropped) element.AddToClassList(LiveOpsHubClassNames.ExportDiffRowDropped);
            if (row.Consequence == LiveEventCalendarConsequence.ProgressLost) element.AddToClassList(LiveOpsHubClassNames.ExportDiffRowProgressLost);

            Label symbol = new Label(row.SymbolText);
            symbol.AddToClassList(LiveOpsHubClassNames.ExportDiffSymbol);
            symbol.AddToClassList(LiveOpsHubClassNames.Mono);
            if (row.Consequence == LiveEventCalendarConsequence.Dropped) LiveOpsHubStyle.SetStateText(symbol, HealthState.Blocked);
            if (row.Consequence == LiveEventCalendarConsequence.ProgressLost) LiveOpsHubStyle.SetStateText(symbol, HealthState.Warning);
            element.Add(symbol);

            VisualElement text = new VisualElement();
            text.AddToClassList(LiveOpsHubClassNames.ExportDiffRowText);
            VisualElement titleRow = new VisualElement();
            titleRow.AddToClassList(LiveOpsHubClassNames.ExportDiffRowTitle);
            titleRow.Add(new Label(row.TitleText));
            if (row.IsReviewRequired)
            {
                Label tag = new Label(LiveOpsHubStrings.ExportDiffRequiredTag);
                tag.AddToClassList(LiveOpsHubClassNames.Tag);
                titleRow.Add(tag);
            }
            text.Add(titleRow);

            Label consequence = new Label(row.ConsequenceText);
            consequence.AddToClassList(LiveOpsHubClassNames.ExportDiffRowConsequence);
            if (row.Consequence == LiveEventCalendarConsequence.Dropped) LiveOpsHubStyle.SetStateText(consequence, HealthState.Blocked);
            if (row.Consequence == LiveEventCalendarConsequence.ProgressLost) LiveOpsHubStyle.SetStateText(consequence, HealthState.Warning);
            text.Add(consequence);
            element.Add(text);

            VisualElement spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList(LiveOpsHubClassNames.ExportCardSpacer);
            element.Add(spacer);

            if (row.HasReviewToggle)
            {
                Toggle toggle = new Toggle(LiveOpsHubStrings.ExportDiffReviewedToggle);
                toggle.AddToClassList(LiveOpsHubClassNames.ExportDiffRowAction);
                toggle.SetValueWithoutNotify(row.IsReviewed);
                toggle.RegisterValueChangedCallback(changeEvent => RaiseReviewToggled(row.Change, changeEvent.newValue));
                element.Add(toggle);
            }
            else if (row.Consequence == LiveEventCalendarConsequence.Dropped)
            {
                Button fix = new Button(() => RaiseFixRequested(row.Change)) { text = LiveOpsHubStrings.ExportDiffFixInValidationLink };
                fix.AddToClassList(LiveOpsHubClassNames.Button);
                fix.AddToClassList(LiveOpsHubClassNames.ExportDiffRowAction);
                element.Add(fix);
            }

            return element;
        }

        private void RaiseReviewToggled(LiveEventCalendarChange change, bool reviewed)
        {
            Action<LiveEventCalendarChange, bool> handler = ReviewToggled;
            if (handler != null) handler(change, reviewed);
        }

        private void RaiseFixRequested(LiveEventCalendarChange change)
        {
            Action<LiveEventCalendarChange> handler = FixRequested;
            if (handler != null) handler(change);
        }

        private void RaiseBackToPublished()
        {
            Action handler = BackToPublishedRequested;
            if (handler != null) handler();
        }
    }
}
