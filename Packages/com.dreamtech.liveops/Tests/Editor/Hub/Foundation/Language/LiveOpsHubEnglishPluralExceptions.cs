using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Sổ ngoại lệ của luật số ít/số nhiều tiếng Anh (Q-W5-2) — mẫu <c>LiveOpsHubCaptureDeferrals</c> (V-5): mọi chỗ KHÔNG
    /// theo luật đều phải có tên và LÝ DO viết ra, không được im lặng bỏ qua.
    /// <para>
    /// <see cref="EnglishPluralTests"/> quét catalog bằng một luật máy duy nhất: câu tiếng Anh có <c>{i}</c> đứng ngay trước
    /// một từ thường kết thúc bằng "s" thì đó là ứng viên khoá đếm. Ứng viên phải MANG DẤU <c>{i|số ít|số nhiều}</c>, hoặc
    /// nằm ở một trong hai danh sách dưới đây.
    /// </para>
    /// </summary>
    internal static class LiveOpsHubEnglishPluralExceptions
    {
        /// <summary>
        /// Từ kết thúc bằng "s" nhưng KHÔNG phải danh từ đếm được: động từ chia ngôi thứ ba, một giới từ và hai đại từ. Gặp
        /// <c>"{0} is running"</c> thì <c>{0}</c> là TÊN đợt, không phải số đếm — chia số ở đó là vô nghĩa.
        /// <para>
        /// Danh sách đóng: thêm một câu tiếng Anh dùng động từ mới ngay sau <c>{i}</c> thì test đỏ cho tới khi người viết
        /// xếp nó vào đây (hoặc đánh dấu câu) — đó chính là chỗ chặn "quên chia số".
        /// </para>
        /// <para>
        /// Từ ở đây chặn cả khi nó đứng XEN GIỮA <c>{i}</c> và danh từ: bộ dò của <see cref="EnglishPluralTests"/> cho phép
        /// tới ba từ chen giữa (Q-W5-2 quét lại, 17/9/2026 — "{0} unsaved changes" từng lọt vì tính từ chen vào), nên gặp
        /// <c>"{0} field still holds an unsaved draft"</c> thì động từ "holds" phải cắt đường dò, không thì mọi câu có
        /// <c>{i}</c> rồi vài từ rồi một động từ đều thành ứng viên giả.
        /// </para>
        /// </summary>
        internal static IReadOnlyList<string> WordsThatAreNotCountedNouns { get; } = Array.AsReadOnly(new[]
        {
            "as", "is", "was", "has", "does", "ends", "runs", "contains", "overlaps", "holds", "this", "its",
        });

        /// <summary>
        /// Khoá có số đếm thật nhưng KHÔNG đánh dấu, kèm lý do. Khoá không còn tồn tại trong catalog thì test đỏ — sổ này
        /// không được phép mục nát theo thời gian.
        /// </summary>
        internal static IReadOnlyDictionary<string, string> KeysWithoutMark { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            {
                nameof(LiveOpsHubStrings.OverviewProgressLostCountFormat),
                "\"progress\" là danh từ không đếm được — tiếng Anh viết \"1 progress lost\" y như \"5 progress lost\"."
            },
            {
                nameof(LiveOpsHubStrings.ShellRailProgressLostCountFormat),
                "\"progress\" là danh từ không đếm được — xem OverviewProgressLostCountFormat."
            },
            {
                nameof(LiveOpsHubStrings.ValidationSummaryProgressLostFormat),
                "\"progress\" là danh từ không đếm được — xem OverviewProgressLostCountFormat."
            },
            {
                nameof(LiveOpsHubStrings.ServicesHealthProgressLostGroupFormat),
                "\"progress\" là danh từ không đếm được — xem OverviewProgressLostCountFormat."
            },
            {
                nameof(LiveOpsHubStrings.OverviewMetricNotCheckedRulesFormat),
                "Màn Tổng quan đã có khuôn số ít RIÊNG (OverviewMetricNotCheckedSingleRuleFormat) và người gọi chọn khuôn theo n; "
                + "đánh dấu thêm ở đây là đẻ đường thứ hai cho cùng một việc."
            },
            {
                nameof(LiveOpsHubStrings.OverviewMetricNotCheckedRemoteRulesFormat),
                "Cặp đôi của OverviewMetricNotCheckedSingleRemoteRuleFormat — xem OverviewMetricNotCheckedRulesFormat."
            },
            {
                nameof(LiveOpsHubStrings.OverviewNoBlockersEmptyFormat),
                "Cặp đôi của OverviewNoBlockersEmptySingleRuleFormat — xem OverviewMetricNotCheckedRulesFormat."
            },
        };
    }
}
