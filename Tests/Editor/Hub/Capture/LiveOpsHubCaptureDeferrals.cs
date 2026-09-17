using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Sổ id kịch bản chụp KHÔNG có ảnh ở bản này, và sổ id chụp thật nhưng không nằm trong bảng hằng (V-5, gói G-ACCEPT).
    /// Cùng một tinh thần với <c>LiveOpsHubEnglishPluralExceptions</c>: chỗ nào lệch khỏi luật chung đều phải có TÊN và LÝ DO
    /// viết ra, không được im lặng bỏ qua — vì bảng hằng <see cref="LiveOpsHubCaptureScenarioIds"/> đóng từ W0 và không gói nào
    /// được xoá hằng ở đó để "cho xanh".
    /// <para>
    /// <see cref="CaptureCoverageTests"/> đọc hai sổ dưới đây. Sổ nào mục nát (id không còn tồn tại, id vừa hoãn vừa có kịch bản,
    /// dòng CHANGELOG đã bị xoá) thì test đỏ — sổ ghi nợ mà không ai kiểm thì chỉ là lời hứa suông.
    /// </para>
    /// </summary>
    internal static class LiveOpsHubCaptureDeferrals
    {
        /// <summary>
        /// Id hằng đã khai nhưng CHƯA có kịch bản chụp ở bản này: khoá = tên hằng trong <see cref="LiveOpsHubCaptureScenarioIds"/>,
        /// giá trị = một mảnh câu PHẢI tìm thấy trong mục "Chưa có ở bản này" của <c>CHANGELOG.md</c>, để người đọc CHANGELOG
        /// biết đúng thứ đang thiếu mà không phải mở mã nguồn.
        /// <para>
        /// <b>Trống ở đợt nghiệm thu W7 (17/9/2026).</b> Đợt W6 đã làm cả hai gói P1-lùi (<c>G-OPT-TIMELINE</c>,
        /// <c>G-OPT-SHORTCUTHELP</c>) nên không id nào của P1 phải hoãn: 109/109 hằng đều có đúng một kịch bản đã đăng ký và
        /// đã chụp đủ hai skin × hai bản Unity. Sổ để trống là trạng thái ĐÚNG, không phải chỗ quên điền.
        /// </para>
        /// </summary>
        internal static IReadOnlyDictionary<string, string> DeferredScenarioIds { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Id kịch bản chụp thật nhưng KHÔNG khai hằng ở <see cref="LiveOpsHubCaptureScenarioIds"/>, kèm lý do. Khoá = id ảnh
        /// (tên file <c>&lt;id&gt;-&lt;skin&gt;.png</c>), giá trị = lý do.
        /// <para>
        /// Luật V-5 muốn mọi id nằm ở đúng một bảng hằng đóng, nên chiều ngược lại (kịch bản có id lạ) cũng phải chặn: nếu không,
        /// một gói sau có thể lặng lẽ thêm ảnh mà ma trận truy vết 9.5 không bao giờ biết. Mục dưới đây là biến thể ngôn ngữ do
        /// PD-45 sinh ra — bộ ảnh ghim là TIẾNG VIỆT (để so với hình thiết kế), kèm một mẫu tiếng Anh; mẫu ấy là ảnh phụ của một
        /// hằng đã có, không phải một hình mới của thiết kế, nên nó không đòi một hằng riêng.
        /// </para>
        /// </summary>
        internal static IReadOnlyDictionary<string, string> ScenarioIdsOutsideTheConstantTable { get; } =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                {
                    "h04-overview-default-en",
                    "Mẫu tiếng Anh theo PD-45: cùng trạng thái với hằng H04OverviewDefault, chỉ đổi ngôn ngữ hiển thị. "
                    + "Biến thể ngôn ngữ của một hình đã ghim thì không phải một hình mới của thiết kế, nên không khai hằng riêng."
                },
            };
    }
}
