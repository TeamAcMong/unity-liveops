using System;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Outcome cho kết quả xuất, đăng, đồng bộ (8.5, [FD §3.9]): dấu Ok/Blocked · dòng chính 11px · dòng phụ 10px · "còn đến khi bạn
    /// làm việc khác" · nút hành động tuỳ chọn. Khác toast ở chỗ không tự tắt: kết quả "Đã copy JSON 5b0d93" là thứ người dùng cần
    /// đọc lại lúc dán vào remote config. View chỉ vẽ <see cref="LiveOpsOutcomeRecord"/>; bản ghi sống trong
    /// <c>LiveOpsHubWindowState</c> để qua domain reload, và việc xoá khi làm việc khác do host quyết (G-HOSTUI).
    /// </summary>
    internal sealed class LiveOpsOutcomeView : VisualElement
    {
        internal const string ElementName = "hub-outcome";
        internal const string HeadlineElementName = "hub-outcome-headline";
        internal const string DetailElementName = "hub-outcome-detail";
        internal const string ActionButtonElementName = "hub-outcome-action";

        private readonly LiveOpsStateMark _mark;
        private readonly Label _headline;
        private readonly Label _detail;
        private readonly Label _footnote;
        private readonly Button _actionButton;

        public LiveOpsOutcomeView()
        {
            name = ElementName;
            AddToClassList(LiveOpsHubClassNames.Outcome);
            AddToClassList(LiveOpsHubClassNames.OutcomeHidden);

            _mark = new LiveOpsStateMark();
            Add(_mark);

            VisualElement text = new VisualElement();
            text.AddToClassList(LiveOpsHubClassNames.OutcomeText);
            _headline = new Label { name = HeadlineElementName };
            _headline.AddToClassList(LiveOpsHubClassNames.OutcomeHeadline);
            text.Add(_headline);
            _detail = new Label { name = DetailElementName };
            _detail.AddToClassList(LiveOpsHubClassNames.OutcomeDetail);
            text.Add(_detail);
            _footnote = new Label(LiveOpsHubStrings.FeedbackOutcomeFootnote);
            _footnote.AddToClassList(LiveOpsHubClassNames.OutcomeFootnote);
            text.Add(_footnote);
            Add(text);

            _actionButton = new Button(OnActionClicked) { name = ActionButtonElementName };
            _actionButton.AddToClassList(LiveOpsHubClassNames.Button);
            _actionButton.AddToClassList(LiveOpsHubClassNames.OutcomeAction);
            _actionButton.AddToClassList(LiveOpsHubClassNames.OutcomeActionHidden);
            Add(_actionButton);
        }

        /// <summary>Nút hành động được bấm: (<see cref="LiveOpsOutcomeRecord.ActionId"/>, <see cref="LiveOpsOutcomeRecord.ActionArgument"/>).</summary>
        public event Action<string, string> ActionInvoked;

        public LiveOpsOutcomeRecord Record { get; private set; }

        internal Label HeadlineLabel => _headline;
        internal Label DetailLabel => _detail;
        internal Button ActionButton => _actionButton;

        /// <param name="actionLabel">Nhãn nút (động từ, vd "Mở thư mục"); "" = không nút dù bản ghi có ActionId — chữ nút thuộc màn tạo kết quả.</param>
        public void SetRecord(LiveOpsOutcomeRecord record, string actionLabel = "")
        {
            if (record == null)
            {
                ClearRecord();
                return;
            }
            Record = record;
            HealthState state = record.IsBlocked ? HealthState.Blocked : HealthState.Ok;
            _mark.SetHealth(state);
            _headline.text = record.Headline;
            _detail.text = record.Detail;
            // Lỗi đọc bằng màu chữ blocked-text; thành công giữ màu chữ thường (dấu Ok đủ nói kết quả).
            LiveOpsHubStyle.SetStateText(_headline, record.IsBlocked ? HealthState.Blocked : HealthState.Ok);
            EnableInClassList(LiveOpsHubClassNames.OutcomeBlocked, record.IsBlocked);
            bool hasAction = record.ActionId.Length > 0 && !string.IsNullOrEmpty(actionLabel);
            _actionButton.text = hasAction ? actionLabel : string.Empty;
            _actionButton.EnableInClassList(LiveOpsHubClassNames.OutcomeActionHidden, !hasAction);
            RemoveFromClassList(LiveOpsHubClassNames.OutcomeHidden);
        }

        /// <summary>Ẩn outcome (host gọi khi có Apply, điều hướng sang màn khác hay hành động xuất khác).</summary>
        public void ClearRecord()
        {
            Record = null;
            _headline.text = string.Empty;
            _detail.text = string.Empty;
            AddToClassList(LiveOpsHubClassNames.OutcomeHidden);
        }

        public bool IsShown => Record != null;

        private void OnActionClicked()
        {
            if (Record == null || Record.ActionId.Length == 0) return;
            ActionInvoked?.Invoke(Record.ActionId, Record.ActionArgument);
        }
    }
}
