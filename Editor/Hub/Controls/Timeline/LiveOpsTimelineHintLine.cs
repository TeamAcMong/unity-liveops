using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Dòng gợi ý 18px ở đáy timeline [SD1 §3.6]: đổi theo ngữ cảnh nghỉ / đang chọn / đang kéo để phím và cử chỉ có đường tìm ra ngay
    /// tại chỗ. Chỉ nhắc thứ đã có ở bản này (I-5: không nhắc Shift kéo theo, chọn dải). Id đợt đang chọn mono đậm, phần mô tả mờ 0,82
    /// + ellipsis. Nhãn phím Edit (⌘D, ⌘⌫) đọc binding thật qua <see cref="LiveOpsHubKeyLabels"/>; không có binding thì bỏ vế đó.
    /// </summary>
    internal sealed class LiveOpsTimelineHintLine : VisualElement
    {
        private const string DuplicateShortcutId = "Main Menu/Edit/Duplicate";
        private const string DeleteShortcutId = "Main Menu/Edit/Delete";

        public LiveOpsTimelineHintLine()
        {
            AddToClassList(LiveOpsHubClassNames.TimelineHint);
            tooltip = LiveOpsHubStrings.TimelineHintTooltip;
            Subject = new Label { pickingMode = PickingMode.Ignore };
            Subject.AddToClassList(LiveOpsHubClassNames.TimelineHintSubject);
            Subject.AddToClassList(LiveOpsHubClassNames.Mono);
            Add(Subject);
            Description = new Label { pickingMode = PickingMode.Ignore };
            Description.AddToClassList(LiveOpsHubClassNames.TimelineHintText);
            Add(Description);
            ShowIdle();
        }

        internal Label Subject { get; }
        internal Label Description { get; }

        internal void ShowIdle()
        {
            string actionKey = Application.platform == RuntimePlatform.OSXEditor
                ? LiveOpsHubStrings.TimelineActionKeyMac
                : LiveOpsHubStrings.TimelineActionKeyOther;
            SetText(string.Empty, string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineHintIdleFormat, actionKey));
        }

        internal void ShowDragging()
        {
            SetText(string.Empty, LiveOpsHubStrings.TimelineHintDragging);
        }

        /// <param name="bar">Thanh đang chọn.</param>
        /// <param name="findingShortLabel">Câu ngắn của phát hiện nặng nhất trên thanh (LiveOpsFindingText, không rich text); "" khi không có.</param>
        internal void ShowSelected(LiveOpsTimelineBarModel bar, string findingShortLabel)
        {
            if (bar == null)
            {
                ShowIdle();
                return;
            }
            string description;
            if (bar.IsStrip)
            {
                description = LiveOpsHubStrings.TimelineHintStripSelected;
            }
            else if (bar.Source != LiveOpsTimelineBarSource.Fixed)
            {
                description = LiveOpsHubStrings.TimelineHintRecurringSelected;
            }
            else if (bar.IsEnded)
            {
                description = LiveOpsHubStrings.TimelineHintEndedSelected;
            }
            else if (bar.IsRunning)
            {
                description = LiveOpsHubStrings.TimelineHintRunningSelected;
            }
            else if (bar.IsDropped && !string.IsNullOrEmpty(findingShortLabel))
            {
                description = string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineHintDroppedFormat, findingShortLabel)
                              + ShortcutPart(DeleteShortcutId, LiveOpsHubStrings.TimelineHintDelete);
            }
            else
            {
                description = LiveOpsHubStrings.TimelineHintFixedSelected + ShortcutPart(DuplicateShortcutId, LiveOpsHubStrings.TimelineHintDuplicate);
            }
            SetText(bar.EventId, description);
        }

        private static string ShortcutPart(string shortcutId, string action)
        {
            string label = LiveOpsHubKeyLabels.For(shortcutId);
            return string.IsNullOrEmpty(label)
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineHintShortcutFormat, label, action);
        }

        private void SetText(string subject, string description)
        {
            Subject.text = subject;
            Subject.EnableInClassList(LiveOpsHubClassNames.TimelineHidden, subject.Length == 0);
            Description.text = description;
        }
    }
}
