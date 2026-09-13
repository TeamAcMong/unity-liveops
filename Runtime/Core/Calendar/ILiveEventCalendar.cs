using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps
{
    /// <summary>
    /// Lịch event: thời điểm nào có đợt nào. Hợp đồng mọi bản cài phải giữ:
    /// <list type="bullet">
    /// <item>Trả các đợt của đúng loại <c>eventType</c> giao với [fromUtc, toUtc), sắp theo <see cref="LiveEventInstance.StartUtc"/>.</item>
    /// <item>Hai đợt cùng loại không chồng nhau — mỗi loại có nhiều nhất một đợt đang chạy.</item>
    /// <item>Cùng một đợt luôn mang cùng id, dù hỏi bằng khung giờ nào (id là khoá dữ liệu của người chơi).</item>
    /// <item>Dữ liệu lịch hỏng (từ remote config) thì bỏ qua mục hỏng, không ném exception.</item>
    /// </list>
    /// </summary>
    public interface ILiveEventCalendar
    {
        IReadOnlyList<LiveEventInstance> GetInstances(string eventType, DateTime fromUtc, DateTime toUtc);

        /// <summary>Các loại event lịch này có thể trả về (bảng debug liệt kê).</summary>
        IReadOnlyCollection<string> EventTypes { get; }
    }
}
