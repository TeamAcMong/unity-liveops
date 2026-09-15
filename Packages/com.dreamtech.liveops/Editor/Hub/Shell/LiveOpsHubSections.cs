using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Registry DUY NHẤT của các màn P1 (PD-1: 6 màn, tầng CHẠY không vẽ). Thứ tự trong danh sách = thứ tự rail = ⌘1…6 = thứ tự
    /// palette ([FD §3.1]) — thêm màn P2/P3 chỉ là thêm một dòng ở đây. G-SESSION đổi <see cref="Create"/> thành nhận services
    /// (chữ ký đóng băng khi cổng W3 xanh).
    /// </summary>
    internal static class LiveOpsHubSections
    {
        /// <summary>Id kebab-case — nằm trong SessionState, trạng thái view đã lưu và điều hướng; không đổi sau phát hành.</summary>
        internal static class Ids
        {
            public const string Overview = "overview";
            public const string EventTypes = "event-types";
            public const string Calendar = "calendar";
            public const string RecurringRules = "recurring";
            public const string Validation = "validation";
            public const string Export = "export";
        }

        /// <summary>Danh sách MỚI mỗi lần gọi: mỗi cửa sổ giữ màn của riêng nó (hai cửa sổ hub không dùng chung view/trạng thái).</summary>
        public static List<IHubSection> Create()
        {
            return new List<IHubSection>
            {
                new OverviewSection(),
                new EventTypesSection(),
                new CalendarSection(),
                new RecurringRulesSection(),
                new ValidationSection(),
                new ExportSection(),
            };
        }
    }
}
