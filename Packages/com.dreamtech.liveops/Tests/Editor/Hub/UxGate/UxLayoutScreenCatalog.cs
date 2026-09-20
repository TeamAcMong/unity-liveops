using System;
using System.Collections.Generic;

namespace DreamTech.LiveOps.Editor.Tests
{
    /// <summary>
    /// Cách một màn của ma trận bố cục có được trạng thái DỮ LIỆU XẤU NHẤT — hoặc lời khai vì sao nó chưa có.
    /// </summary>
    internal enum UxWorstCaseCoverage
    {
        /// <summary>Màn tự dựng services trên tài liệu xấu nhất / rỗng / chưa có bản đã đăng (<c>WithServices</c>).</summary>
        Services,

        /// <summary>Màn đứng trên mẫu thiết kế nhưng CHỌN tới đúng phần xấu của mẫu (đợt bị bỏ, đợt có giờ không đọc được).</summary>
        Selection,

        /// <summary>Màn đo cùng một vùng cây với một màn khác đã đứng trên dữ liệu xấu nhất — khai tên màn ấy.</summary>
        CoveredBy,

        /// <summary>Màn CHƯA có biến thể xấu nhất; phải khai mã phiếu và nói rõ còn thiếu trạng thái nào.</summary>
        Ticket,
    }

    /// <summary>Một dòng của bảng kê: màn nào, thuộc section nào, và trạng thái phủ dữ liệu xấu nhất của nó.</summary>
    internal sealed class UxLayoutScreenRegistration
    {
        public UxLayoutScreenRegistration(string id, string sectionId, UxWorstCaseCoverage coverage, string reference, string note)
        {
            Id = id;
            SectionId = sectionId;
            Coverage = coverage;
            Reference = reference;
            Note = note;
        }

        public string Id { get; }
        public string SectionId { get; }
        public UxWorstCaseCoverage Coverage { get; }

        /// <summary>Tên màn phủ hộ (<see cref="UxWorstCaseCoverage.CoveredBy"/>) hoặc mã phiếu (<see cref="UxWorstCaseCoverage.Ticket"/>); rỗng với hai loại còn lại.</summary>
        public string Reference { get; }

        /// <summary>Câu người đọc: màn này nhìn thấy trạng thái xấu nào, hoặc còn THIẾU trạng thái xấu nào.</summary>
        public string Note { get; }
    }

    /// <summary>
    /// Bảng kê MỌI màn của ma trận kiểm bố cục cùng cách nó chạm tới dữ liệu XẤU NHẤT.
    /// <para>
    /// Vì sao bảng này tồn tại (soát W10 R-04): luật "mỗi màn phải có ít nhất một biến thể ngày tồi tệ nhất" của đợt W10 chỉ
    /// sống trong MỘT khối chú thích của <see cref="UxLayoutAuditTests"/> — không câu assert nào bắt buộc, nên nó là lời khuyên
    /// chứ không phải cái lưới. Mà chính cái lưới ấy là thứ ba lỗi W9-29/30/31 đã lọt qua: 24 màn cũ đều đứng trên tài liệu ĐẸP.
    /// </para>
    /// <para>
    /// Bảng kê KHÔNG phải danh sách miễn trừ. Nó không tha một phát hiện nào; nó chỉ bắt người thêm màn phải trả lời một câu —
    /// "màn này nhìn thấy dữ liệu xấu ở đâu" — và khoá câu trả lời "chưa có" vào một tập ĐÃ ĐẾM
    /// (<see cref="ScreensWithoutWorstCaseVariant"/>) để tập ấy không phình âm thầm.
    /// </para>
    /// <para>
    /// <see cref="UxLayoutAuditTests.RunScreen"/> đối chiếu từng màn đang chạy với bảng này, nên thêm màn mà quên khai là ĐỎ,
    /// và khai "có services" mà màn không khai <c>WithServices</c> (hoặc ngược lại) cũng ĐỎ — hai chiều, để lời khai không
    /// trôi khỏi mã.
    /// </para>
    /// </summary>
    internal static class UxLayoutScreenCatalog
    {
        /// <summary>
        /// Mã phiếu của phần LỖ HỔNG ma trận còn nợ sau đợt W10: 12 màn dưới đây chưa có biến thể dữ liệu xấu nhất của riêng
        /// chúng. (Mã W10-07 chứ không phải W10-04: W10-01…W10-05 đã được bàn giao ở plan/w10/G-W10-MATRIX-build.md §6.)
        /// Dùng CHUNG một mã vì đây là một việc — "dựng trạng thái xấu cho nhóm màn tách nguyên nhân và hai cửa sổ
        /// phụ" — chứ không phải 12 việc rời nhau; phần nào của nó còn thiếu thì đọc ở <see cref="UxLayoutScreenRegistration.Note"/>.
        /// </summary>
        internal const string WorstCaseGapTicketId = "W10-07";

        /// <summary>
        /// Tập màn được phép CHƯA có biến thể dữ liệu xấu nhất, đúng như đợt W10 đã đếm. Khoá cả tập (không chỉ kiểm từng mục)
        /// vì kiểm từng mục cho phép dòng thứ mười ba lặng lẽ xuất hiện với một câu ghi chú dài quá 40 ký tự — đúng đường lách
        /// mà danh sách HOÃN của đợt W9 đã phải bịt.
        /// </summary>
        internal static readonly string[] ScreensWithoutWorstCaseVariant =
        {
            "shell-rail-status",
            "calendar-multi-selection",
            "calendar-body-fills",
            "calendar-lane-viewport",
            "timeline-ruler-track",
            "calendar-medium-drawer",
            "calendar-toast",
            "timeline-ruler-labels-zoom-in",
            "timeline-ruler-labels-zoom-out",
            "timeline-legend-contrast",
            "add-event-popover",
            "confirm-window",
        };

        private static readonly List<UxLayoutScreenRegistration> Entries = new List<UxLayoutScreenRegistration>
        {
            // ---------------------------------------------------------------------------------- màn đầy đủ của sáu section
            Covered("overview", LiveOpsHubSections.Ids.Overview, "overview-worst-data",
                "cùng thân màn Tổng quan, bản kia đứng trên id dài + việc cần làm nhiều + số đếm lớn"),
            Covered("event-types", LiveOpsHubSections.Ids.EventTypes, "event-types-worst-data",
                "cùng bảng loại, bản kia có loại CHƯA KHAI nên dấu trạng thái mới được vẽ"),
            Covered("calendar-no-selection", LiveOpsHubSections.Ids.Calendar, "calendar-worst-data",
                "cùng cửa sổ màn Lịch, bản kia đứng trên tài liệu xấu nhất ở đủ bảy cỡ"),
            Covered("calendar-selection", LiveOpsHubSections.Ids.Calendar, "calendar-worst-data",
                "cùng cửa sổ màn Lịch khi ĐANG chọn một thanh, bản kia chọn thanh có id dài nhất"),
            Ticket("calendar-multi-selection", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: chọn NHIỀU thanh trên tài liệu xấu nhất — dòng gộp của nhiều đợt id dài là câu dài nhất mà "
                + "màn này vẽ, và chưa lượt nào dựng nó"),
            Covered("recurring-default", LiveOpsHubSections.Ids.RecurringRules, "recurring-worst-data",
                "cùng thân màn Luật lặp, bản kia đứng trên tiền tố id dài nhất và chu kỳ 8760 giờ"),
            Covered("validation", LiveOpsHubSections.Ids.Validation, "validation-worst-data",
                "cùng thân màn Kiểm lịch, bản kia có phát hiện với câu dài nhất"),
            Covered("export", LiveOpsHubSections.Ids.Export, "export-worst-data",
                "cùng thân màn Xuất JSON, bản kia đứng trên số byte bảy chữ số và sha 64 ký tự"),
            Ticket("shell-rail-status", LiveOpsHubSections.Ids.Overview,
                "còn thiếu: khung (rail + chân trang) khi câu trạng thái DÀI NHẤT — tên tài liệu dài, số phát hiện ba chữ "
                + "số — vì chân trang là chỗ duy nhất chữ đó xuất hiện"),

            // ---------------------------------------------------------------------------------- màn đứng trên dữ liệu xấu
            Selection("calendar-selection-dropped", LiveOpsHubSections.Ids.Calendar,
                "chọn đợt BỊ BỎ của mẫu — biến thể dài nhất của dòng gợi ý đáy trục (W9-29)"),
            Selection("calendar-inspector-fielderror", LiveOpsHubSections.Ids.Calendar,
                "chọn đợt có giờ kết thúc KHÔNG ĐỌC ĐƯỢC của mẫu — hàng field mới dựng biểu tượng lỗi (W9-30)"),
            Services("calendar-worst-data", LiveOpsHubSections.Ids.Calendar,
                "tài liệu xấu nhất: id đợt dài nhất, tên loại dài nhất, một loại chưa khai, một giờ hỏng"),
            Services("calendar-empty", LiveOpsHubSections.Ids.Calendar,
                "lịch RỖNG: không đợt, không luật, không loại — trục và inspector dựng template trạng thái rỗng"),
            Services("recurring-worst-data", LiveOpsHubSections.Ids.RecurringRules,
                "tiền tố id dài nhất, chu kỳ 8760 giờ, một lần chạy 999 giờ"),
            Services("recurring-empty", LiveOpsHubSections.Ids.RecurringRules,
                "danh sách luật RỖNG — thân màn đổi sang khối 'chưa có luật nào' thay vì bảng"),
            Services("overview-worst-data", LiveOpsHubSections.Ids.Overview,
                "id đợt dài nhất, danh sách việc cần làm dài nhất, số đếm ba chữ số trên thẻ tóm tắt"),
            Services("overview-empty", LiveOpsHubSections.Ids.Overview,
                "không đợt sắp diễn ra, không việc cần làm — thẻ đổi sang template overview-empty-body"),
            Services("event-types-worst-data", LiveOpsHubSections.Ids.EventTypes,
                "có loại CHƯA KHAI (dấu trạng thái mới được vẽ), id loại và khoá config dài nhất"),
            Services("event-types-empty", LiveOpsHubSections.Ids.EventTypes,
                "bảng loại RỖNG — không hàng nào, bảng chỉ còn hàng tiêu đề cột"),
            Services("validation-worst-data", LiveOpsHubSections.Ids.Validation,
                "phát hiện có câu DÀI NHẤT: loại chưa khai + giờ không đọc được + id đợt dài nhất"),
            Services("validation-empty", LiveOpsHubSections.Ids.Validation,
                "0 phát hiện — màn chỉ còn câu 'mọi thứ ổn' và thanh công cụ lọc"),
            Services("export-worst-data", LiveOpsHubSections.Ids.Export, "số byte bảy chữ số, ghi chú dài, sha 64 ký tự"),
            Services("export-no-baseline", LiveOpsHubSections.Ids.Export, "CHƯA có bản đã đăng — không có bản nào để so"),

            // ---------------------------------------------------------------------------------- màn tách một nguyên nhân
            Ticket("calendar-body-fills", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: chuỗi calendar-root → calendar-main trên tài liệu xấu nhất; luật đang đo là luật GIÃN nên dữ "
                + "liệu chưa đổi kết quả, nhưng lịch RỖNG dựng template khác và đó mới là chỗ chuỗi này dễ đứt"),
            Ticket("calendar-lane-viewport", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: vùng làn khi số làn LỚN NHẤT (mỗi loại một làn, gồm loại chưa khai) — số làn là thứ quyết định "
                + "chiều cao vùng làn, và mẫu đẹp chỉ có năm làn"),
            Ticket("timeline-ruler-track", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: track thước khi khoảng ngày RỘNG NHẤT của tài liệu xấu nhất — bề rộng track theo khoảng ngày, "
                + "nên mẫu đẹp đo đúng một khoảng duy nhất"),
            Covered("calendar-medium-no-selection", LiveOpsHubSections.Ids.Calendar, "calendar-worst-data",
                "cùng cửa sổ màn Lịch ở 820x560, bản kia chạy đủ bảy cỡ trong đó có 820x560"),
            Ticket("calendar-medium-drawer", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: ngăn kéo inspector ở 820 khi đợt được chọn có id dài nhất + giờ hỏng — ngăn kéo hẹp hơn pane "
                + "thường nên đúng chỗ ấy chữ dài mới chạm mép"),
            Covered("calendar-inspector", LiveOpsHubSections.Ids.Calendar, "calendar-inspector-fielderror",
                "cùng nhánh inspector, bản kia chọn đợt có giờ không đọc được"),
            Ticket("calendar-toast", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: toast mang câu DÀI NHẤT (tên đợt id dài + số giờ bốn chữ số); câu ngắn thì toast không bao giờ "
                + "đủ rộng để chạm chân trang, tức luật không-đè chưa từng bị thử thật"),
            Covered("calendar-lane-meta", LiveOpsHubSections.Ids.Calendar, "calendar-worst-data",
                "cùng dòng thông tin làn, bản kia có tên loại dài nhất và loại chưa khai"),
            Covered("calendar-toolbar-narrow", LiveOpsHubSections.Ids.Calendar, "calendar-worst-data",
                "cùng toolbar màn Lịch ở cỡ hẹp, bản kia chạy 700 và 820 trên tài liệu xấu nhất"),
            Covered("timeline-ruler-labels-default", LiveOpsHubSections.Ids.Calendar, "calendar-worst-data",
                "cùng nhãn thước ở mức thu phóng mặc định, bản kia quét cả cửa sổ trên tài liệu xấu nhất"),
            Ticket("timeline-ruler-labels-zoom-in", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: phóng to trên tài liệu xấu nhất — nhãn dày nhất sinh ra ở mức phóng to, và không màn nào vừa "
                + "phóng to vừa đứng trên khoảng ngày rộng nhất"),
            Ticket("timeline-ruler-labels-zoom-out", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: thu nhỏ trên tài liệu xấu nhất — đây là mức sinh nhãn THÁNG (chuỗi dài nhất của thước) và là "
                + "một trong bảy chỗ chữ sát mép ô mà W9-25 đếm"),
            Ticket("timeline-legend-contrast", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: chú giải khi có loại CHƯA KHAI — dấu màu của loại chưa khai là dấu duy nhất khai màu CÓ ALPHA, "
                + "đúng loại dấu từng lọt lưới tương phản trước bản vá hợp thành"),

            // ---------------------------------------------------------------------------------- cửa sổ PHỤ
            Ticket("add-event-popover", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: popover Thêm đợt khi dropdown tên loại mang TÊN DÀI NHẤT — popover rộng cố định 320px nên tên "
                + "loại dài là chỗ duy nhất nó có thể vỡ"),
            Ticket("confirm-window", LiveOpsHubSections.Ids.Calendar,
                "còn thiếu: hộp xác nhận khi câu hỏi IN ID ĐỢT dài nhất — hộp rộng cố định, nên id dài là chỗ duy nhất "
                + "chữ của nó chạm mép"),
        };

        internal static IReadOnlyList<UxLayoutScreenRegistration> All
        {
            get { return Entries; }
        }

        /// <summary>Dòng của màn <paramref name="screenId"/>; null khi màn chưa được khai.</summary>
        internal static UxLayoutScreenRegistration Find(string screenId)
        {
            for (int index = 0; index < Entries.Count; index++)
            {
                if (string.Equals(Entries[index].Id, screenId, StringComparison.Ordinal)) return Entries[index];
            }

            return null;
        }

        private static UxLayoutScreenRegistration Services(string id, string sectionId, string note)
        {
            return new UxLayoutScreenRegistration(id, sectionId, UxWorstCaseCoverage.Services, string.Empty, note);
        }

        private static UxLayoutScreenRegistration Selection(string id, string sectionId, string note)
        {
            return new UxLayoutScreenRegistration(id, sectionId, UxWorstCaseCoverage.Selection, string.Empty, note);
        }

        private static UxLayoutScreenRegistration Covered(string id, string sectionId, string coveringScreenId, string note)
        {
            return new UxLayoutScreenRegistration(id, sectionId, UxWorstCaseCoverage.CoveredBy, coveringScreenId, note);
        }

        private static UxLayoutScreenRegistration Ticket(string id, string sectionId, string note)
        {
            return new UxLayoutScreenRegistration(id, sectionId, UxWorstCaseCoverage.Ticket, WorstCaseGapTicketId, note);
        }
    }
}
