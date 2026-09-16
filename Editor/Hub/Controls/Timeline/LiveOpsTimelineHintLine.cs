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

        /// <summary>
        /// (nợ D-3(b), Hình 12 khung 13) Con trỏ đang đứng trên chỗ trống của một làn cố định: gợi ý nêu TÊN LÀN, khoảng đang
        /// xem và cách tạo đợt ngay tại đó — "star-tournament · 14/9 → 19/9 · nhấp đúp chỗ trống để thêm đợt". Trước W5 dòng gợi
        /// ý chỉ có ba trạng thái (nghỉ / đang kéo / đang chọn) nên khung 13 vẽ y hệt khung 1 và cử chỉ nhấp đúp — đường DUY
        /// NHẤT tạo đợt bằng chuột ở chỗ trống — không có chỗ nào nói ra.
        /// </summary>
        /// <param name="laneDisplayName">Tên làn đang trỏ tới; rỗng thì về trạng thái nghỉ (không có làn thì không có gợi ý làn).</param>
        /// <param name="rangeText">Khoảng đang xem, đã định dạng ngắn ("14/9 → 19/9").</param>
        internal void ShowEmptyLane(string laneDisplayName, string rangeText)
        {
            if (string.IsNullOrEmpty(laneDisplayName))
            {
                ShowIdle();
                return;
            }
            SetText(laneDisplayName, string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.TimelineHintEmptyLaneFormat,
                rangeText ?? string.Empty));
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
