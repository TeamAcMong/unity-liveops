using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor
{
    /// <summary>
    /// Registry DUY NHẤT của các màn P1 (PD-1: 6 màn, tầng CHẠY không vẽ). Thứ tự trong danh sách = thứ tự rail = ⌘1…6 = thứ tự
    /// palette ([FD §3.1]) — thêm màn P2/P3 chỉ là thêm một dòng ở đây. <see cref="Create(LiveOpsHubServices)"/> nhận services của cửa sổ
    /// (G-SESSION; chữ ký đóng băng khi cổng W3 xanh).
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

        /// <summary>
        /// Danh sách MỚI mỗi lần gọi: mỗi cửa sổ giữ màn của riêng nó (hai cửa sổ hub không dùng chung view/trạng thái). Mọi màn nhận
        /// services của cửa sổ qua ctor — màn không tự dựng phiên hay adapter.
        /// </summary>
        public static List<IHubSection> Create(LiveOpsHubServices services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            return new List<IHubSection>
            {
                new OverviewSection(services),
                new EventTypesSection(services),
                new CalendarSection(services),
                new RecurringRulesSection(services),
                new ValidationSection(services),
                new ExportSection(services),
            };
        }

        /// <summary>
        /// Registry với services KHÔNG có asset, không tìm asset, không tự kiểm — cho nơi chỉ cần hình dạng registry (test hợp đồng màn,
        /// kịch bản chụp khung của W2). Overload này giữ chữ ký W2 để các file đó (thuộc quyền ghi của G-SHELL) không phải đổi cùng đợt.
        /// Phiên không asset không đăng ký sự kiện tĩnh nào nên không cần Dispose.
        /// <para>
        /// BẪY: nơi gọi <c>LiveOpsHubWindow.OpenForTest(LiveOpsHubSections.Create(), …)</c> có HAI phiên — màn cầm phiên dựng ở đây, cửa
        /// sổ dựng phiên riêng của nó. Vô hại ở W3 vì màn giữ chỗ chưa đọc phiên; màn thật của W4 phải mở bằng
        /// <c>OpenWithServices(services, …)</c> + <see cref="Create(LiveOpsHubServices)"/> trên CÙNG một services.
        /// </para>
        /// </summary>
        internal static List<IHubSection> Create()
        {
            return Create(new LiveOpsHubServicesBuilder().WithCalendarAsset(null).WithAutoCheckOnOpen(false).Build());
        }
    }
}
