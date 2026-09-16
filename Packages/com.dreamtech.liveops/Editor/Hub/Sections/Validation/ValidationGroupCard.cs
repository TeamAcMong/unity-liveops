using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Card một nhóm hậu quả của màn Kiểm lịch ([SD2 §2.1]): header (dấu · tiêu đề · meta · tag "cũ") → blurb → các hàng.
    /// Hai nhóm cuối ("n luật đã qua", "Đã bỏ qua (n)") thu gọn mặc định và chỉ có một dòng mono liệt kê id.
    /// </summary>
    internal sealed class ValidationGroupCard : VisualElement
    {
        internal ValidationGroupCard(ValidationGroup group, bool isStale)
        {
            Group = group ?? throw new ArgumentNullException(nameof(group));
            AddToClassList(LiveOpsHubClassNames.Card);
            AddToClassList(LiveOpsHubClassNames.ValidationGroupCard);
            AddToClassList(GroupClassOf(group.Kind));

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CardHeader);

            LiveOpsStateMark mark = new LiveOpsStateMark();
            mark.SetHealth(group.State);
            mark.Size = LiveOpsStateMark.MarkSize.Small;
            header.Add(mark);

            if (group.IsCollapsible)
            {
                Chevron = new LiveOpsChevron();
                header.Add(Chevron);
            }

            TitleLabel = new Label(group.Title);
            TitleLabel.AddToClassList(LiveOpsHubClassNames.ValidationGroupTitle);
            TitleLabel.AddToClassList(TextClassOf(group.State));
            header.Add(TitleLabel);

            if (group.MetaText.Length > 0)
            {
                MetaLabel = new Label(group.MetaText);
                MetaLabel.AddToClassList(LiveOpsHubClassNames.ValidationGroupMeta);
                header.Add(MetaLabel);
            }

            if (isStale)
            {
                StaleTag = new Label(LiveOpsHubStrings.ValidationStaleTag);
                StaleTag.AddToClassList(LiveOpsHubClassNames.Tag);
                StaleTag.AddToClassList(LiveOpsHubClassNames.ValidationStaleTag);
                header.Add(StaleTag);
            }
            Add(header);

            if (group.Blurb.Length > 0)
            {
                BlurbLabel = new Label(group.Blurb);
                BlurbLabel.AddToClassList(LiveOpsHubClassNames.ValidationGroupBlurb);
                Add(BlurbLabel);
            }

            Body = new VisualElement();
            Body.AddToClassList(LiveOpsHubClassNames.ValidationGroupRows);
            Add(Body);

            if (group.IsCollapsible)
            {
                header.AddToClassList(LiveOpsHubClassNames.CardHeaderClickable);
                header.RegisterCallback<PointerDownEvent>(OnHeaderPointerDown);
                Label collapsedDetail = new Label(group.CollapsedDetailText);
                collapsedDetail.AddToClassList(LiveOpsHubClassNames.ValidationPassedIds);
                collapsedDetail.AddToClassList(LiveOpsHubClassNames.Mono);
                Body.Add(collapsedDetail);
                SetCollapsed(true);
            }
            else
            {
                BuildRows(group.Rows);
            }
        }

        /// <summary>Hàng trong card yêu cầu một việc — màn nối tiếp lên phiên.</summary>
        internal event Action<ValidationRow> RowActionRequested;

        internal event Action<ValidationRow> RowLinkRequested;
        internal event Action<ValidationRow> RowSelectionRequested;

        internal ValidationGroup Group { get; }
        internal Label TitleLabel { get; }
        internal Label MetaLabel { get; }
        internal Label BlurbLabel { get; }
        internal Label StaleTag { get; }
        internal LiveOpsChevron Chevron { get; }
        internal VisualElement Body { get; }
        internal IReadOnlyList<ValidationFindingRow> Rows => _rows;
        internal bool IsCollapsed => ClassListContains(LiveOpsHubClassNames.CardCollapsed);

        private readonly List<ValidationFindingRow> _rows = new List<ValidationFindingRow>();

        internal void SetSelectedRow(string selectionKey)
        {
            for (int index = 0; index < _rows.Count; index++)
            {
                _rows[index].SetSelected(selectionKey.Length > 0 && string.Equals(_rows[index].Row.SelectionKey, selectionKey, StringComparison.Ordinal));
            }
        }

        internal void SetCollapsed(bool collapsed)
        {
            EnableInClassList(LiveOpsHubClassNames.CardCollapsed, collapsed);
            Body.EnableInClassList(LiveOpsHubClassNames.ValidationHidden, collapsed);
            // Chevron chỉ sang phải khi thu gọn, xuống khi mở — cùng quy ước với Foldout của Unity.
            if (Chevron != null) Chevron.Direction = collapsed ? LiveOpsChevron.ChevronDirection.Right : LiveOpsChevron.ChevronDirection.Down;
        }

        private void OnHeaderPointerDown(PointerDownEvent pointerEvent)
        {
            SetCollapsed(!IsCollapsed);
        }

        private void BuildRows(IReadOnlyList<ValidationRow> rows)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                ValidationFindingRow row = new ValidationFindingRow(rows[index], StaleTag != null);
                row.ActionRequested += finding => RowActionRequested?.Invoke(finding);
                row.LinkRequested += finding => RowLinkRequested?.Invoke(finding);
                row.SelectionRequested += finding => RowSelectionRequested?.Invoke(finding);
                if (index == rows.Count - 1) row.MarkAsLast();
                Body.Add(row);
                _rows.Add(row);
            }
        }

        private static string GroupClassOf(ValidationGroupKind kind)
        {
            switch (kind)
            {
                case ValidationGroupKind.Dropped: return LiveOpsHubClassNames.ValidationGroupDropped;
                case ValidationGroupKind.ProgressLost: return LiveOpsHubClassNames.ValidationGroupProgressLost;
                case ValidationGroupKind.ShouldReview: return LiveOpsHubClassNames.ValidationGroupShouldReview;
                case ValidationGroupKind.NotMeasured: return LiveOpsHubClassNames.ValidationGroupNotMeasured;
                case ValidationGroupKind.Passed: return LiveOpsHubClassNames.ValidationGroupPassed;
                default: return LiveOpsHubClassNames.ValidationGroupIgnored;
            }
        }

        private static string TextClassOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Blocked: return LiveOpsHubClassNames.TextBlocked;
                case HealthState.Warning: return LiveOpsHubClassNames.TextWarning;
                case HealthState.NotMeasured: return LiveOpsHubClassNames.TextQuiet;
                default: return LiveOpsHubClassNames.TextQuiet;
            }
        }
    }
}
