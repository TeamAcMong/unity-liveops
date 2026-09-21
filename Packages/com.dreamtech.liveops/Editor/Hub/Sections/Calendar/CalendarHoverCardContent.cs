using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UIElements;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Nội dung hover card của một thanh đợt [SD1 §3.8]: key–value 260px (cột key 58), một dòng lỗi và chân nhắc cử chỉ. Lớp chỉ
    /// DỰNG CÂY — việc chờ 500ms, ẩn 100ms và ghim do <see cref="LiveOpsHoverCardHost"/> dùng chung của cửa sổ lo.
    /// <para>
    /// (V-22 CC-FT-1) Dòng lỗi lấy câu từ <see cref="LiveOpsFindingText"/> bằng overload CÓ NGỮ CẢNH (<c>latestStamp</c>,
    /// <c>nowUtc</c>) — cùng câu mà màn Kiểm lịch in cho đúng phát hiện đó, nên hai màn không bao giờ mô tả một lỗi hai kiểu.
    /// </para>
    /// <para>
    /// Thẻ đến bằng F8 thì ĐƯỢC GHIM và có thêm hàng "Sửa nhanh…" + bộ đếm "1/5"; thẻ đến bằng chuột thì không. Lý do: thẻ chuột
    /// biến mất khi rời chuột nên một cái nút trên đó là cái bẫy — kéo chuột tới thì thẻ đã tắt.
    /// </para>
    /// </summary>
    internal static class CalendarHoverCardContent
    {
        /// <summary>Bề rộng thẻ [SD1 §3.8] — khai ở đây vì kịch bản chụp đo đúng con số này.</summary>
        internal const float CardWidth = 260f;

        /// <summary>Thẻ theo chuột: key–value + dòng lỗi + chân, không nút.</summary>
        public static VisualElement Build(LiveOpsTimelineBarModel bar, LiveEventCalendarFinding finding, LiveOpsHubServices services,
            PublishedCalendarStamp latestStamp)
        {
            return Build(bar, finding, services, latestStamp, false, 0, 0, null);
        }

        /// <param name="isPinned">true = đến bằng F8: thêm hàng "Sửa nhanh…" + bộ đếm.</param>
        /// <param name="findingIndex">Thứ tự phát hiện đang xem, đếm từ 1.</param>
        /// <param name="findingCount">Tổng phát hiện của CẢ LỊCH — khớp số của màn Kiểm lịch, không phải số của một làn.</param>
        public static VisualElement Build(LiveOpsTimelineBarModel bar, LiveEventCalendarFinding finding, LiveOpsHubServices services,
            PublishedCalendarStamp latestStamp, bool isPinned, int findingIndex, int findingCount, Action quickFix)
        {
            if (bar == null) throw new ArgumentNullException(nameof(bar));
            if (services == null) throw new ArgumentNullException(nameof(services));
            LiveOpsHubFormat format = services.Format;

            VisualElement card = new VisualElement { name = LiveOpsHubPaths.CalendarDepthElementNames.HoverCard };
            card.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverCard);

            VisualElement header = new VisualElement();
            header.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverHeader);
            Label eventId = new Label(bar.EventId);
            eventId.AddToClassList(LiveOpsHubClassNames.Mono);
            header.Add(eventId);
            Label phase = new Label(PhaseTextOf(bar));
            phase.AddToClassList(LiveOpsHubClassNames.Tag);
            header.Add(phase);
            card.Add(header);

            card.Add(KeyValueRow(LiveOpsHubStrings.CalendarDepthHoverStartLabel, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarDepthHoverUtcWithDeviceFormat, format.ShortDateTime(bar.StartUtc),
                format.DeviceClock(bar.StartUtc) + " " + LiveOpsHubStrings.DeviceTimeSuffix)));
            card.Add(KeyValueRow(LiveOpsHubStrings.CalendarDepthHoverEndLabel, string.Format(CultureInfo.InvariantCulture,
                LiveOpsHubStrings.CalendarDepthHoverEndValueFormat, format.ShortDateTime(bar.EndUtc),
                // (UX-28, V11/UJ-20) Dạng ĐỦ CHỮ: thẻ rộng 260px và người đọc đang dừng chuột để đọc kỹ — "1n 12g" là dạng của
                // readout khi kéo, chỗ duy nhất thật sự thiếu chỗ.
                format.Duration(bar.EndUtc - bar.StartUtc, false))));

            if (finding != null) card.Add(ProblemRow(finding, format, latestStamp, services.Clock.UtcNow));

            if (isPinned) card.Add(PinnedRow(findingIndex, findingCount, quickFix));

            Label footer = new Label(LiveOpsHubStrings.CalendarDepthHoverFooter);
            footer.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverFooter);
            card.Add(footer);
            return card;
        }

        /// <summary>
        /// Dòng lỗi [SD1 §3.8]: dấu trạng thái 12px ("icon err 12") rồi mới tới câu 10px theo họ màu của hậu quả. Dấu là
        /// <see cref="LiveOpsStateMark"/> bản <c>Large</c> — cùng thứ mà hàng pane "So với đã đăng" và card lỗi dùng, nên một
        /// người đọc thẻ không phải học thêm ký hiệu nào.
        /// </summary>
        private static VisualElement ProblemRow(LiveEventCalendarFinding finding, LiveOpsHubFormat format,
            PublishedCalendarStamp latestStamp, DateTime nowUtc)
        {
            HealthState health = LiveOpsHubFindingRouting.StateOf(finding.Consequence);
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverProblemRow);

            LiveOpsStateMark mark = new LiveOpsStateMark { Size = LiveOpsStateMark.MarkSize.Large };
            mark.SetHealth(health);
            mark.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverProblemMark);
            row.Add(mark);

            Label problem = new Label(LiveOpsFindingText.Headline(finding, format, latestStamp))
            {
                name = LiveOpsHubPaths.CalendarDepthElementNames.HoverProblem,
                tooltip = LiveOpsFindingText.Meta(finding, format, nowUtc),
            };
            problem.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverProblem);
            LiveOpsHubStyle.SetStateText(problem, health);
            row.Add(problem);
            return row;
        }

        /// <summary>Hàng chỉ có ở thẻ ghim: nút "Sửa nhanh…" bên trái, bộ đếm "1/5" bên phải.</summary>
        private static VisualElement PinnedRow(int findingIndex, int findingCount, Action quickFix)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverPinnedRow);
            Button button = new Button(() => quickFix?.Invoke())
            {
                text = LiveOpsHubStrings.CalendarDepthHoverQuickFixButton,
                name = LiveOpsHubPaths.CalendarDepthElementNames.HoverQuickFix,
            };
            button.AddToClassList(LiveOpsHubClassNames.Button);
            row.Add(button);
            Label counter = new Label(string.Format(CultureInfo.InvariantCulture, LiveOpsHubStrings.CalendarDepthHoverCounterFormat,
                findingIndex.ToString(CultureInfo.InvariantCulture), findingCount.ToString(CultureInfo.InvariantCulture)))
            {
                name = LiveOpsHubPaths.CalendarDepthElementNames.HoverCounter,
            };
            counter.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverCounter);
            row.Add(counter);
            return row;
        }

        private static VisualElement KeyValueRow(string key, string value)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverRow);
            Label keyLabel = new Label(key);
            keyLabel.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverKey);
            row.Add(keyLabel);
            Label valueLabel = new Label(value);
            valueLabel.AddToClassList(LiveOpsHubClassNames.CalendarDepthHoverValue);
            row.Add(valueLabel);
            return row;
        }

        /// <summary>Tag giai đoạn của thẻ dùng ĐÚNG bốn chữ của inspector đợt — hai chỗ nói về cùng một đợt thì phải cùng chữ.</summary>
        private static string PhaseTextOf(LiveOpsTimelineBarModel bar)
        {
            if (bar.IsRunning) return LiveOpsHubStrings.CalendarPhaseRunning;
            if (bar.IsEnded) return LiveOpsHubStrings.CalendarPhaseEnded;
            return LiveOpsHubStrings.CalendarPhaseUpcoming;
        }

        /// <summary>
        /// Phát hiện của cả lịch theo thứ tự F8 đi qua: dùng chung một danh sách cho thẻ ghim, bộ đếm "n/m" và bước nhảy — ba
        /// thứ đó đếm lệch nhau là cách nhanh nhất để "1/5" nói dối.
        /// </summary>
        public static IReadOnlyList<LiveEventCalendarFinding> FindingsInOrder(LiveOpsHubCalendarSession session)
        {
            List<LiveEventCalendarFinding> result = new List<LiveEventCalendarFinding>();
            LiveEventCalendarCheckReport report = session == null || session.Check == null ? null : session.Check.LastReport;
            if (report == null) return result;
            IReadOnlyList<LiveEventCalendarFinding> findings = report.Findings;
            for (int index = 0; index < findings.Count; index++) result.Add(findings[index]);
            return result;
        }
    }
}
