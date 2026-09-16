using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Một hàng phát hiện của màn Kiểm lịch — đúng anatomy [SD2 §2.2]: sọc mức độ 3px · icon Console 16px (hoặc vòng rỗng
    /// <see cref="LiveOpsStateMark"/> khi chưa kiểm) · headline + meta + id luật mono · khối nút căn phải.
    /// <para>
    /// Hàng KHÔNG tự viết câu và KHÔNG tự sửa lịch: chữ đã nằm sẵn trong <see cref="ValidationRow"/> (từ
    /// <see cref="LiveOpsFindingText"/>, V-8) và mọi thao tác phát lên sự kiện để màn quyết — nhờ vậy test hàng không cần phiên.
    /// </para>
    /// </summary>
    internal sealed class ValidationFindingRow : VisualElement
    {
        internal ValidationFindingRow(ValidationRow row, bool isStale)
        {
            Row = row ?? throw new ArgumentNullException(nameof(row));
            AddToClassList(LiveOpsHubClassNames.FindingRow);
            focusable = true;

            VisualElement stripe = new VisualElement { name = LiveOpsHubPaths.ValidationElementNames.RowStripe };
            stripe.AddToClassList(LiveOpsHubClassNames.ValidationRowStripe);
            stripe.AddToClassList(StripeClassOf(row.State));
            Add(stripe);

            Add(BuildLeadingMark(row.State));

            VisualElement text = new VisualElement();
            text.AddToClassList(LiveOpsHubClassNames.ValidationRowText);
            Headline = new Label(row.Headline) { enableRichText = true };
            Headline.AddToClassList(LiveOpsHubClassNames.ValidationRowHeadline);
            Headline.AddToClassList(TextClassOf(row.State));
            if (row.TagText.Length > 0)
            {
                // Chỉ bọc khi CÓ tag: hàng không tag giữ nguyên cây của W4 nên ảnh đã chụp không đổi một pixel nào.
                VisualElement headlineLine = new VisualElement();
                headlineLine.AddToClassList(LiveOpsHubClassNames.ValidationRowHeadlineLine);
                headlineLine.Add(Headline);
                TagLabel = new Label(row.TagText);
                TagLabel.AddToClassList(LiveOpsHubClassNames.Tag);
                headlineLine.Add(TagLabel);
                text.Add(headlineLine);
            }
            else
            {
                text.Add(Headline);
            }

            MetaLabel = new Label(row.MetaText) { enableRichText = true };
            MetaLabel.AddToClassList(LiveOpsHubClassNames.ValidationRowMeta);
            text.Add(MetaLabel);

            RuleIdLabel = new Label(row.RuleIdLine) { enableRichText = true };
            RuleIdLabel.AddToClassList(LiveOpsHubClassNames.ValidationRowRuleId);
            RuleIdLabel.AddToClassList(LiveOpsHubClassNames.Mono);
            text.Add(RuleIdLabel);

            // (V-21 CC-VALB-3) Luật không có lệnh sửa: câu chỉ cách sửa tay phải đọc được ngay trên hàng, không giấu trong tooltip.
            if (row.ManualFixSentence.Length > 0)
            {
                ManualFixLabel = new Label(row.ManualFixSentence) { enableRichText = true };
                ManualFixLabel.AddToClassList(LiveOpsHubClassNames.ValidationRowManualFix);
                text.Add(ManualFixLabel);
            }
            Add(text);

            Actions = new VisualElement();
            VisualElement actions = Actions;
            actions.AddToClassList(LiveOpsHubClassNames.ValidationRowActions);
            // "Quyết định… ▾" là MENU chứ không phải nút ([SD2 §2.4]) và menu cần phiên để áp, nên màn cắm nó vào sau
            // (SetDecisionMenu). Dựng một nút chết ở đây rồi thay là chỗ dễ để sót một nút bấm không làm gì.
            if (row.Action != ValidationRowAction.None && row.Action != ValidationRowAction.Decision && row.ActionText.Length > 0)
            {
                ActionButton = new Button(OnActionClicked) { text = row.ActionText, tooltip = row.ActionTooltip };
                ActionButton.AddToClassList(LiveOpsHubClassNames.Button);
                actions.Add(ActionButton);
            }
            if (row.LinkText.Length > 0)
            {
                LinkButton = new Button(OnLinkClicked) { text = row.LinkText };
                LinkButton.AddToClassList(LiveOpsHubClassNames.ValidationRowLink);
                actions.Add(LinkButton);
            }
            Add(actions);

            if (isStale) AddToClassList(LiveOpsHubClassNames.ValidationRowStale);
            RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        /// <summary>Người dùng bấm động từ chính của hàng (Sửa, Đề xuất…, Xem diff, Copy lỗi…).</summary>
        internal event Action<ValidationRow> ActionRequested;

        /// <summary>Người dùng bấm link phụ ("Xem trong lịch", "Mở luật").</summary>
        internal event Action<ValidationRow> LinkRequested;

        /// <summary>Hàng được chọn (bấm chuột) — màn mở pane Chi tiết.</summary>
        internal event Action<ValidationRow> SelectionRequested;

        internal ValidationRow Row { get; }
        internal Label Headline { get; }

        /// <summary>Tag cạnh headline ("đã tới hẹn", "hẹn tới 14/9 00:00"); null khi hàng không có tag.</summary>
        internal Label TagLabel { get; }

        /// <summary>Khối nút căn phải — màn cắm <see cref="DecisionMenu"/> vào đây cho hàng "Quyết định… ▾".</summary>
        internal VisualElement Actions { get; private set; }
        internal Label MetaLabel { get; }
        internal Label RuleIdLabel { get; }
        internal Label ManualFixLabel { get; }
        internal Button ActionButton { get; }
        internal Button LinkButton { get; }

        internal void SetSelected(bool selected)
        {
            EnableInClassList(LiveOpsHubClassNames.ValidationRowSelected, selected);
            EnableInClassList(LiveOpsHubClassNames.RowActive, selected);
        }

        /// <summary>Cắm menu "Quyết định… ▾" vào đầu khối nút; gọi lại lần nữa thì thay menu cũ, không chồng hai menu.</summary>
        internal void SetDecisionMenu(VisualElement menu)
        {
            if (menu == null) throw new ArgumentNullException(nameof(menu));
            if (DecisionMenuElement != null) DecisionMenuElement.RemoveFromHierarchy();
            DecisionMenuElement = menu;
            Actions.Insert(0, menu);
        }

        internal VisualElement DecisionMenuElement { get; private set; }

        /// <summary>Hàng cuối card mang <c>--last</c> do C# gắn: USS của Unity không có <c>:last-child</c> ([SD2 §2.2]).</summary>
        internal void MarkAsLast()
        {
            AddToClassList(LiveOpsHubClassNames.RowLast);
        }

        private void OnActionClicked()
        {
            SelectionRequested?.Invoke(Row);
            ActionRequested?.Invoke(Row);
        }

        private void OnLinkClicked()
        {
            LinkRequested?.Invoke(Row);
        }

        private void OnPointerDown(PointerDownEvent pointerEvent)
        {
            SelectionRequested?.Invoke(Row);
            // Nhấp đúp = "Xem trong lịch" (mục 7.5 Tương tác): cùng đích với link phụ, nên phát cùng một sự kiện để chỉ có một
            // chỗ quyết định đi đâu. Hàng không có link (Bỏ qua cảnh báo… ẩn ở W4) vẫn mở được đích bằng nhấp đúp.
            if (pointerEvent.clickCount >= DoubleClickCount) LinkRequested?.Invoke(Row);
        }

        private const int DoubleClickCount = 2;

        private static VisualElement BuildLeadingMark(HealthState state)
        {
            if (state == HealthState.NotMeasured)
            {
                // Chưa kiểm không có icon Console tương ứng: vòng rỗng của họ sức khoẻ là hình DUY NHẤT nói "không đo được".
                LiveOpsStateMark mark = new LiveOpsStateMark();
                mark.SetHealth(HealthState.NotMeasured);
                mark.AddToClassList(LiveOpsHubClassNames.ValidationRowIcon);
                return mark;
            }

            Image icon = LiveOpsHubIcons.CreateImage(state == HealthState.Blocked ? BlockedIconName : WarningIconName, IconSize);
            icon.AddToClassList(LiveOpsHubClassNames.ValidationRowIcon);
            return icon;
        }

        private const string BlockedIconName = "console.erroricon.sml";
        private const string WarningIconName = "console.warnicon.sml";
        private const int IconSize = 16;

        private static string StripeClassOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Blocked: return LiveOpsHubClassNames.FindingStripeBlocked;
                case HealthState.NotMeasured: return LiveOpsHubClassNames.FindingStripeNotMeasured;
                default: return LiveOpsHubClassNames.FindingStripeWarning;
            }
        }

        private static string TextClassOf(HealthState state)
        {
            switch (state)
            {
                case HealthState.Blocked: return LiveOpsHubClassNames.TextBlocked;
                case HealthState.NotMeasured: return LiveOpsHubClassNames.TextQuiet;
                default: return LiveOpsHubClassNames.TextWarning;
            }
        }
    }
}
