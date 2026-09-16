using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// View mỏng của card "Việc cần làm trước khi đăng" [SD1 §1.1]: mỗi hàng = sọc 3px theo mức → dấu trạng thái → khối chữ
    /// (headline 11px + dòng "ở đâu" 10px) → cột 96px "chặn Copy JSON" (dấu Blocked 7px riêng, vì đó là trạng thái CỔNG) →
    /// nút 150px nêu đích. Không tự viết câu nào: mọi chữ đã có trong <see cref="OverviewNeedsActionRow"/>.
    /// <para>
    /// Trạng thái (c) "không còn việc chặn" thay danh sách bằng empty + hai nút, NHƯNG vẫn liệt kê các hàng NotMeasured bên
    /// dưới kèm caption — chưa kiểm không có nghĩa là ổn [SD1 §1.4].
    /// </para>
    /// </summary>
    internal sealed class OverviewNeedsActionList
    {
        internal const string EmptyElementName = "overview-needs-action-empty";
        internal const string RowElementNamePrefix = "overview-need-row-";
        internal const string RowButtonElementNamePrefix = "overview-need-button-";
        internal const string RowButtonSlotElementNamePrefix = "overview-need-slot-";
        internal const string CaptionElementName = "overview-needs-action-caption";
        internal const string RowBlocksElementNamePrefix = "overview-need-blocks-";

        private readonly VisualElement _container;
        private readonly Action<OverviewNeedsActionRow> _rowActivated;
        private readonly Action<LiveOpsHubNavigation> _navigate;
        private readonly Func<OverviewNeedsActionRow, string> _disabledReasonOf;

        /// <param name="rowActivated">Nút của hàng bị bấm — màn quyết định làm gì (điều hướng, kiểm lại, dán JSON).</param>
        /// <param name="navigate">Hai nút của empty (c) đi thẳng tới màn, không qua hàng nào.</param>
        /// <param name="disabledReasonOf">Câu vì sao nút của hàng đang khoá — bắt buộc có khi khoá ([FD §2.16]).</param>
        internal OverviewNeedsActionList(VisualElement container, Action<OverviewNeedsActionRow> rowActivated,
            Action<LiveOpsHubNavigation> navigate, Func<OverviewNeedsActionRow, string> disabledReasonOf)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _rowActivated = rowActivated ?? throw new ArgumentNullException(nameof(rowActivated));
            _navigate = navigate ?? throw new ArgumentNullException(nameof(navigate));
            _disabledReasonOf = disabledReasonOf ?? throw new ArgumentNullException(nameof(disabledReasonOf));
        }

        /// <param name="showEmpty">true = trạng thái (c): empty thay danh sách, hàng NotMeasured vẫn liệt kê.</param>
        internal void Rebuild(IReadOnlyList<OverviewNeedsActionRow> rows, bool showEmpty, int notMeasuredRuleCount)
        {
            _container.Clear();
            if (showEmpty) _container.Add(BuildEmpty(notMeasuredRuleCount));

            int index = 0;
            foreach (OverviewNeedsActionRow row in rows)
            {
                _container.Add(BuildRow(row, index));
                index++;
            }

            if (!showEmpty || rows.Count == 0) return;
            Label caption = new Label(LiveOpsHubStrings.OverviewNotCheckedStillListedCaption) { name = CaptionElementName };
            caption.AddToClassList(LiveOpsHubClassNames.Caption);
            _container.Add(caption);
        }

        private VisualElement BuildEmpty(int notMeasuredRuleCount)
        {
            VisualElement empty = new VisualElement { name = EmptyElementName };
            empty.AddToClassList(LiveOpsHubClassNames.Empty);

            // Số ít / số nhiều: tiếng Việt một bản, tiếng Anh hai bản ("1 rule" chứ không "1 rules") — câu này in ngay trên
            // ảnh mẫu tiếng Anh mà user đọc để duyệt bản dịch.
            string title = notMeasuredRuleCount > 0
                ? string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    notMeasuredRuleCount == 1
                        ? LiveOpsHubStrings.OverviewNoBlockersEmptySingleRuleFormat
                        : LiveOpsHubStrings.OverviewNoBlockersEmptyFormat, notMeasuredRuleCount)
                : LiveOpsHubStrings.OverviewNoBlockersEmptyNoRules;
            Label titleLabel = new Label(title);
            titleLabel.AddToClassList(LiveOpsHubClassNames.EmptyTitle);
            empty.Add(titleLabel);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.OverviewEmptyActions);
            actions.Add(BuildNavigateButton(LiveOpsHubStrings.OverviewOpenValidationButton,
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Validation), true));
            actions.Add(BuildNavigateButton(LiveOpsHubStrings.OverviewOpenExportButton,
                LiveOpsHubNavigation.To(LiveOpsHubSections.Ids.Export), false));
            empty.Add(actions);
            return empty;
        }

        private Button BuildNavigateButton(string text, LiveOpsHubNavigation navigation, bool isFirst)
        {
            Button button = new Button(() => _navigate(navigation)) { text = text };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            if (isFirst) button.AddToClassList(LiveOpsHubClassNames.ButtonFirst);
            return button;
        }

        private VisualElement BuildRow(OverviewNeedsActionRow row, int index)
        {
            VisualElement element = new VisualElement { name = RowElementNamePrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            element.AddToClassList(LiveOpsHubClassNames.FindingRow);
            element.AddToClassList(LiveOpsHubClassNames.OverviewNeedRow);
            LiveOpsHubStyle.SetSeverityStripe(element, row.State);

            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetHealth(row.State);
            element.Add(mark);

            VisualElement text = new VisualElement();
            text.AddToClassList(LiveOpsHubClassNames.OverviewNeedText);
            Label title = new Label(row.Title);
            title.AddToClassList(LiveOpsHubClassNames.OverviewNeedTitle);
            title.tooltip = LiveOpsFindingText.PlainText(row.Title);
            text.Add(title);
            Label detail = new Label(row.Detail);
            detail.AddToClassList(LiveOpsHubClassNames.OverviewNeedDetail);
            // Câu meta đầy đủ của phát hiện (khoảng giờ, id liên quan) nằm ở tooltip: dòng "ở đâu" chỉ đủ chỗ cho tên màn.
            detail.tooltip = row.DetailTooltip;
            text.Add(detail);
            element.Add(text);

            element.Add(BuildBlocksColumn(row, index));
            element.Add(BuildRowButton(row, index));
            return element;
        }

        /// <summary>Cột 96px: dấu Blocked 7px + "chặn Copy JSON". Hàng không chặn vẫn giữ cột (rỗng) để mọi nút thẳng hàng.</summary>
        private static VisualElement BuildBlocksColumn(OverviewNeedsActionRow row, int index)
        {
            // Đặt tên để lệnh chụp ghi được worldBound: số đo 96px của cột cổng là một hàng của ma trận 9.5.
            VisualElement blocks = new VisualElement
            {
                name = RowBlocksElementNamePrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            blocks.AddToClassList(LiveOpsHubClassNames.OverviewNeedBlocks);
            if (!row.BlocksCopy) return blocks;

            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.Size = LiveOpsStateMark.MarkSize.Small;
            mark.SetHealth(HealthState.Blocked);
            blocks.Add(mark);
            Label label = new Label(LiveOpsHubStrings.OverviewBlocksCopyLabel);
            LiveOpsHubStyle.SetStateText(label, HealthState.Blocked);
            blocks.Add(label);
            return blocks;
        }

        private VisualElement BuildRowButton(OverviewNeedsActionRow row, int index)
        {
            Button button = new Button(() => _rowActivated(row))
            {
                name = RowButtonElementNamePrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
                text = row.ButtonText,
            };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            LiveOpsButtonSlot slot = new LiveOpsButtonSlot(button)
            {
                name = RowButtonSlotElementNamePrefix + index.ToString(System.Globalization.CultureInfo.InvariantCulture),
            };
            slot.AddToClassList(LiveOpsHubClassNames.OverviewNeedButton);
            // Nút khoá LUÔN in lý do thành chữ cạnh nút (SPIKE-B SP-3) — tooltip chỉ là đường phụ.
            LiveOpsHubStyle.SetEnabledWithReason(slot, row.IsButtonEnabled, _disabledReasonOf(row));
            return slot;
        }
    }
}
