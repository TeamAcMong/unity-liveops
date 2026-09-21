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
            // Card có TÊN để lệnh chụp ghi được số đo của nó (9.5): card "Đã bỏ qua" phải đúng 170px, card "đã qua" giãn phần
            // còn lại — hai số của [SD2 §2.1] chỉ kiểm được trên ảnh khi element có tên.
            name = ElementNameOf(group.Kind);
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
                // Nhóm "Đã bỏ qua" mở ra là DANH SÁCH mục (V-15); nhóm "n luật đã qua" chỉ có một dòng mono liệt kê id.
                if (group.Kind == ValidationGroupKind.Ignored && group.Rows.Count > 0) BuildIgnoredRows(group.Rows);
                else
                {
                    Label collapsedDetail = new Label(group.CollapsedDetailText);
                    collapsedDetail.AddToClassList(LiveOpsHubClassNames.ValidationPassedIds);
                    collapsedDetail.AddToClassList(LiveOpsHubClassNames.Mono);
                    Body.Add(collapsedDetail);
                }
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

        /// <summary>"Xem ghi chú" của một mục đã bỏ qua: màn ghim hover card lên chính hàng đó, nên sự kiện mang cả hàng.</summary>
        internal event Action<VisualElement, ValidationRow> IgnoredNoteRequested;

        /// <summary>"Copy mô tả lỗi" của menu chuột phải hàng phát hiện.</summary>
        internal event Action<ValidationRow> RowCopyDescriptionRequested;

        /// <summary>"Mở tài liệu luật &lt;id&gt;" của menu chuột phải hàng phát hiện.</summary>
        internal event Action<ValidationRow> RowOpenRuleDocumentationRequested;

        internal ValidationGroup Group { get; }
        internal Label TitleLabel { get; }
        internal Label MetaLabel { get; }
        internal Label BlurbLabel { get; }
        internal Label StaleTag { get; }
        internal LiveOpsChevron Chevron { get; }
        internal VisualElement Body { get; }
        internal IReadOnlyList<ValidationFindingRow> Rows => _rows;

        /// <summary>Hàng của nhóm "Đã bỏ qua" (V-15) — khác họ với hàng phát hiện nên không nằm chung danh sách F8.</summary>
        internal IReadOnlyList<ValidationIgnoredRow> IgnoredRows => _ignoredRows;

        internal bool IsCollapsed => ClassListContains(LiveOpsHubClassNames.CardCollapsed);

        private readonly List<ValidationFindingRow> _rows = new List<ValidationFindingRow>();
        private readonly List<ValidationIgnoredRow> _ignoredRows = new List<ValidationIgnoredRow>();

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
            // 170px của [SD2 §2.1] là số đo lúc THU GỌN. Mở ra là một danh sách mục: giữ 170px thì meta vỡ sáu dòng đứt giữa
            // từ và ghi chú vỡ ba dòng (soát W5 F-1), nên card xuống hàng riêng và chiếm cả bề ngang.
            if (Group.Kind == ValidationGroupKind.Ignored) EnableInClassList(LiveOpsHubClassNames.ValidationGroupIgnoredOpen, !collapsed);
            Body.EnableInClassList(LiveOpsHubClassNames.ValidationHidden, collapsed);
            // Chevron chỉ sang phải khi thu gọn, xuống khi mở — cùng quy ước với Foldout của Unity.
            if (Chevron != null) Chevron.Direction = collapsed ? LiveOpsChevron.ChevronDirection.Right : LiveOpsChevron.ChevronDirection.Down;
        }

        private void OnHeaderPointerDown(PointerDownEvent pointerEvent)
        {
            SetCollapsed(!IsCollapsed);
        }

        private void BuildIgnoredRows(IReadOnlyList<ValidationRow> rows)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                ValidationIgnoredRow row = new ValidationIgnoredRow(rows[index]);
                row.UnignoreRequested += ignored => RowActionRequested?.Invoke(ignored);
                row.ViewInCalendarRequested += ignored => RowLinkRequested?.Invoke(ignored);
                row.ViewNoteRequested += ignored => IgnoredNoteRequested?.Invoke(row, ignored);
                if (index == rows.Count - 1) row.AddToClassList(LiveOpsHubClassNames.RowLast);
                Body.Add(row);
                _ignoredRows.Add(row);
            }
        }

        private void BuildRows(IReadOnlyList<ValidationRow> rows)
        {
            for (int index = 0; index < rows.Count; index++)
            {
                ValidationFindingRow row = new ValidationFindingRow(rows[index], StaleTag != null);
                row.ActionRequested += finding => RowActionRequested?.Invoke(finding);
                row.LinkRequested += finding => RowLinkRequested?.Invoke(finding);
                row.SelectionRequested += finding => RowSelectionRequested?.Invoke(finding);
                row.CopyDescriptionRequested += finding => RowCopyDescriptionRequested?.Invoke(finding);
                row.OpenRuleDocumentationRequested += finding => RowOpenRuleDocumentationRequested?.Invoke(finding);
                if (index == rows.Count - 1) row.MarkAsLast();
                Body.Add(row);
                _rows.Add(row);
            }
        }

        private static string ElementNameOf(ValidationGroupKind kind)
        {
            switch (kind)
            {
                case ValidationGroupKind.Dropped: return LiveOpsHubPaths.ValidationElementNames.GroupCardPrefix + "dropped";
                case ValidationGroupKind.ProgressLost: return LiveOpsHubPaths.ValidationElementNames.GroupCardPrefix + "progress-lost";
                case ValidationGroupKind.ShouldReview: return LiveOpsHubPaths.ValidationElementNames.GroupCardPrefix + "should-review";
                case ValidationGroupKind.NotMeasured: return LiveOpsHubPaths.ValidationElementNames.GroupCardPrefix + "not-measured";
                case ValidationGroupKind.Passed: return LiveOpsHubPaths.ValidationElementNames.GroupCardPrefix + "passed";
                default: return LiveOpsHubPaths.ValidationElementNames.GroupCardPrefix + "ignored";
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

    /// <summary>
    /// Một mục trong nhóm "Đã bỏ qua (n) ▸" (V-15, [SD2 §2.7] ô 6): meta "luật · đích · khoảng" · ghi chú rút gọn · tag hạn
    /// (nếu là ghi chú hẹn giờ) · nút nhỏ "Bỏ bỏ qua".
    /// <para>
    /// Nút nhỏ là đường đi CHÍNH, menu chuột phải chỉ là đường phụ: menu gốc của Unity không chụp được (S-24), nên một tính
    /// năng chỉ tới được bằng chuột phải là tính năng không ảnh nào chứng minh được là còn sống.
    /// </para>
    /// </summary>
    internal sealed class ValidationIgnoredRow : VisualElement
    {
        internal ValidationIgnoredRow(ValidationRow row)
        {
            Row = row ?? throw new ArgumentNullException(nameof(row));
            AddToClassList(LiveOpsHubClassNames.FindingRow);
            AddToClassList(LiveOpsHubClassNames.ValidationIgnoredRow);

            VisualElement text = new VisualElement();
            text.AddToClassList(LiveOpsHubClassNames.ValidationRowText);

            VisualElement metaLine = new VisualElement();
            metaLine.AddToClassList(LiveOpsHubClassNames.ValidationRowHeadlineLine);
            MetaLabel = new Label(row.MetaText) { enableRichText = true };
            MetaLabel.AddToClassList(LiveOpsHubClassNames.ValidationRowMeta);
            metaLine.Add(MetaLabel);
            if (row.TagText.Length > 0)
            {
                TagLabel = new Label(row.TagText);
                TagLabel.AddToClassList(LiveOpsHubClassNames.Tag);
                metaLine.Add(TagLabel);
            }
            text.Add(metaLine);

            NoteLabel = new Label(row.Headline) { enableRichText = true };
            NoteLabel.AddToClassList(LiveOpsHubClassNames.ValidationIgnoredNote);
            text.Add(NoteLabel);
            Add(text);

            VisualElement actions = new VisualElement();
            actions.AddToClassList(LiveOpsHubClassNames.ValidationRowActions);
            UnignoreButton = new Button(() => UnignoreRequested?.Invoke(Row)) { text = row.ActionText };
            UnignoreButton.AddToClassList(LiveOpsHubClassNames.Button);
            actions.Add(UnignoreButton);
            Add(actions);

            this.AddManipulator(new ContextualMenuManipulator(PopulateFromEvent));
        }

        /// <summary>"Bỏ bỏ qua" — gỡ cảnh báo khỏi asset, không hỏi lại (bảng 7.0): toast Hoàn tác là đường lùi.</summary>
        internal event Action<ValidationRow> UnignoreRequested;

        internal event Action<ValidationRow> ViewNoteRequested;
        internal event Action<ValidationRow> ViewInCalendarRequested;

        internal ValidationRow Row { get; }
        internal Label MetaLabel { get; }
        internal Label NoteLabel { get; }
        internal Label TagLabel { get; }
        internal Button UnignoreButton { get; }

        /// <summary>Menu chuột phải: "Bỏ bỏ qua · Xem ghi chú · Xem trong lịch" ([SD2 §2.7]). Test đọc thẳng hàm này (S-24).</summary>
        internal void PopulateContextMenu(DropdownMenu menu)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            menu.AppendAction(LiveOpsHubStrings.ValidationDepthUnignoreButton,
                action => UnignoreRequested?.Invoke(Row), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction(LiveOpsHubStrings.ValidationDepthViewNoteMenuItem,
                action => ViewNoteRequested?.Invoke(Row), DropdownMenuAction.AlwaysEnabled);
            menu.AppendAction(LiveOpsHubStrings.FindingLinkViewInCalendar,
                action => ViewInCalendarRequested?.Invoke(Row), DropdownMenuAction.AlwaysEnabled);
        }

        private void PopulateFromEvent(ContextualMenuPopulateEvent populateEvent)
        {
            PopulateContextMenu(populateEvent.menu);
        }
    }
}
